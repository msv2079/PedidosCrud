using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.Tokens;

namespace PedidosCrud.Tests;

[TestClass]
public sealed class ApiTests
{
    private ApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    [TestInitialize]
    public void Initialize()
    {
        _factory = new ApplicationFactory();
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    [TestCleanup]
    public void Cleanup()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    private async Task Authenticate()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { username = _factory.Username, password = _factory.Password });
        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<JsonElement>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.GetProperty("accessToken").GetString());
    }

    private static object ValidInput(long? version = null)
    {
        return new { customerName = "Cliente API", description = "Integração HTTP", totalAmount = 1234.56, status = "Open", version };
    }

    [TestMethod]
    public async Task ApiRequiresAuthenticationAndMvcRedirectsToLogin()
    {
        var apiResponse = await _client.GetAsync("/api/pedidos");
        Assert.AreEqual(HttpStatusCode.Unauthorized, apiResponse.StatusCode);
        var pageResponse = await _client.GetAsync("/");
        Assert.AreEqual(HttpStatusCode.Redirect, pageResponse.StatusCode);
        Assert.AreEqual("/account/login", pageResponse.Headers.Location?.ToString());
    }

    [TestMethod]
    public async Task LoginRejectsInvalidCredentials()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { username = _factory.Username, password = Guid.NewGuid().ToString("N") });
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task CrudReturnsCorrectStatusCodesAndPersistsChanges()
    {
        await Authenticate();
        var createResponse = await _client.PostAsJsonAsync("/api/pedidos", ValidInput());
        Assert.AreEqual(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var location = createResponse.Headers.Location!;
        var id = created.GetProperty("id").GetInt32();
        Assert.IsTrue(location.ToString().EndsWith($"/api/pedidos/{id}"));

        var getResponse = await _client.GetAsync(location);
        Assert.AreEqual(HttpStatusCode.OK, getResponse.StatusCode);
        var updateResponse = await _client.PutAsJsonAsync(location, ValidInput(version: 1));
        Assert.AreEqual(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual(2L, updated.GetProperty("version").GetInt64());
        var deleteResponse = await _client.DeleteAsync($"{location}?version=2");
        Assert.AreEqual(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, (await _client.GetAsync(location)).StatusCode);
    }

    [TestMethod]
    public async Task DataTableReturnsFilteredCountsAndRequestedSlice()
    {
        await Authenticate();
        var created = await _client.PostAsJsonAsync("/api/pedidos", new { customerName = "ExclusivoDataTable", description = "Filtro de teste", totalAmount = 10, status = "Paid" });
        created.EnsureSuccessStatusCode();
        var response = await _client.GetAsync("/api/pedidos/datatable?draw=7&start=0&length=10&search[value]=exclusivodatatable&order[0][column]=1&order[0][dir]=asc");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual(7, result.GetProperty("draw").GetInt32());
        Assert.AreEqual(10_001, result.GetProperty("recordsTotal").GetInt32());
        Assert.AreEqual(1, result.GetProperty("recordsFiltered").GetInt32());
        Assert.AreEqual("ExclusivoDataTable", result.GetProperty("data")[0].GetProperty("customerName").GetString());
    }

    [TestMethod]
    public async Task InvalidInputsReturnErrorsAndDoNotCreateOrders()
    {
        await Authenticate();
        var response = await _client.PostAsJsonAsync("/api/pedidos", new { customerName = "", description = "", totalAmount = -1, status = "Open" });
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.IsTrue(problem.GetProperty("errors").EnumerateObject().Count() >= 3);

        var precisionResponse = await _client.PostAsJsonAsync("/api/pedidos", new { customerName = "Cliente", description = "Teste", totalAmount = 1.001, status = "Open" });
        Assert.AreEqual(HttpStatusCode.BadRequest, precisionResponse.StatusCode);
        var list = await _client.GetFromJsonAsync<JsonElement>("/api/pedidos");
        Assert.AreEqual(10_000, list.GetProperty("totalCount").GetInt32());
    }

    [TestMethod]
    public async Task MalformedDataTableRequestReturnsBadRequest()
    {
        await Authenticate();
        var response = await _client.GetAsync("/api/pedidos/datatable?draw=abc&start=0&length=10");
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        var unlimitedResponse = await _client.GetAsync("/api/pedidos/datatable?draw=1&start=0&length=-1");
        Assert.AreEqual(HttpStatusCode.BadRequest, unlimitedResponse.StatusCode);
    }

    [TestMethod]
    public async Task StaleUpdateAndDeleteReturnConflict()
    {
        await Authenticate();
        var created = await (await _client.PostAsJsonAsync("/api/pedidos", ValidInput())).Content.ReadFromJsonAsync<JsonElement>();
        var url = $"/api/pedidos/{created.GetProperty("id").GetInt32()}";
        (await _client.PutAsJsonAsync(url, ValidInput(1))).EnsureSuccessStatusCode();
        Assert.AreEqual(HttpStatusCode.Conflict, (await _client.PutAsJsonAsync(url, ValidInput(1))).StatusCode);
        Assert.AreEqual(HttpStatusCode.Conflict, (await _client.DeleteAsync($"{url}?version=1")).StatusCode);
    }

    [TestMethod]
    public async Task ExpiredAndWrongSignatureTokensAreRejected()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_factory.SigningKey));
        var expired = new JwtSecurityToken("PedidosCrud", "PedidosCrud.Client", expires: DateTime.UtcNow.AddMinutes(-10), signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(expired));
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await _client.GetAsync("/api/pedidos")).StatusCode);

        var wrongKey = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(48));
        var invalid = new JwtSecurityToken("PedidosCrud", "PedidosCrud.Client", expires: DateTime.UtcNow.AddMinutes(10), signingCredentials: new SigningCredentials(wrongKey, SecurityAlgorithms.HmacSha256));
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(invalid));
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await _client.GetAsync("/api/pedidos")).StatusCode);
    }

    [TestMethod]
    public async Task CookieAuthenticationRequiresCsrfForWritesAndLogoutClearsCookie()
    {
        var loginHtml = await _client.GetStringAsync("/account/login");
        var token = ExtractAntiforgeryToken(loginHtml);
        var loginResponse = await _client.PostAsync("/account/login", new FormUrlEncodedContent(new Dictionary<string, string> { ["Username"] = _factory.Username, ["Password"] = _factory.Password, ["__RequestVerificationToken"] = token }));
        Assert.AreEqual(HttpStatusCode.Redirect, loginResponse.StatusCode);
        Assert.IsTrue(loginResponse.Headers.GetValues("Set-Cookie").Any(cookie => cookie.Contains("httponly", StringComparison.OrdinalIgnoreCase)));
        Assert.AreEqual(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync("/api/pedidos", ValidInput())).StatusCode);

        var page = await _client.GetStringAsync("/");
        var csrfToken = ExtractAntiforgeryToken(page);
        _client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrfToken);
        Assert.AreEqual(HttpStatusCode.Created, (await _client.PostAsJsonAsync("/api/pedidos", ValidInput())).StatusCode);
        var logoutResponse = await _client.PostAsync("/account/logout", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = csrfToken }));
        Assert.AreEqual(HttpStatusCode.Redirect, logoutResponse.StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await _client.GetAsync("/api/pedidos")).StatusCode);
    }

    [TestMethod]
    public async Task SimultaneousHttpCreatesHaveUniqueIdsAndCorrectCount()
    {
        await Authenticate();
        var requests = Enumerable.Range(0, 40).Select(async _ =>
        {
            var response = await _client.PostAsJsonAsync("/api/pedidos", ValidInput());
            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        });

        var ids = await Task.WhenAll(requests);
        Assert.AreEqual(40, ids.Distinct().Count());
        var result = await _client.GetFromJsonAsync<JsonElement>("/api/pedidos");
        Assert.AreEqual(10_040, result.GetProperty("totalCount").GetInt32());
    }

    private static string ExtractAntiforgeryToken(string html)
    {
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.IsTrue(match.Success, "A página deve fornecer o token antiforgery.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }
}
