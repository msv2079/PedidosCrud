using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

var root = FindProjectRoot();
var artifacts = Path.Combine(root, "artifacts", "browser");
Directory.CreateDirectory(artifacts);
var username = $"browser-{Guid.NewGuid():N}";
var password = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
var signingKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
var port = GetAvailablePort();
var baseUrl = $"http://127.0.0.1:{port}";
var executable = Environment.GetEnvironmentVariable("BROWSER_EXECUTABLE") ?? FindChrome();
var publishedDirectory = Environment.GetEnvironmentVariable("PUBLISHED_APP_DIRECTORY");
var appDirectory = publishedDirectory is null ? Path.Combine(root, "src", "PedidosCrud.Web") : Path.GetFullPath(publishedDirectory);
var applicationDll = publishedDirectory is null ? Path.Combine(appDirectory, "bin", "Debug", "net10.0", "PedidosCrud.Web.dll") : Path.Combine(appDirectory, "PedidosCrud.Web.dll");
var serverInfo = new ProcessStartInfo("dotnet") { WorkingDirectory = appDirectory, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
serverInfo.ArgumentList.Add(applicationDll);
serverInfo.Environment["ASPNETCORE_URLS"] = baseUrl;
serverInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
serverInfo.Environment["Authentication__Username"] = username;
serverInfo.Environment["Authentication__Password"] = password;
serverInfo.Environment["Authentication__SigningKey"] = signingKey;
using var server = Process.Start(serverInfo) ?? throw new InvalidOperationException("Não foi possível iniciar a aplicação.");
var serverOutput = server.StandardOutput.ReadToEndAsync();
var serverErrors = server.StandardError.ReadToEndAsync();
using var httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
IPage? page = null;
IBrowser? browser = null;
IPlaywright? playwright = null;

try
{
    await WaitForServer(httpClient, server);
    playwright = await Playwright.CreateAsync();
    browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true, ExecutablePath = executable });
    var context = await browser.NewContextAsync(new BrowserNewContextOptions { ViewportSize = new ViewportSize { Width = 1440, Height = 1000 }, Locale = "pt-BR" });
    page = await context.NewPageAsync();
    var javascriptErrors = new List<string>();
    page.PageError += (_, error) => javascriptErrors.Add(error);

    await page.GotoAsync(baseUrl);
    await Expect(page.Locator("#login-form")).ToBeVisibleAsync();
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(artifacts, "login.png"), FullPage = true });
    await page.Locator("#Username").FillAsync(username);
    await page.Locator("#Password").FillAsync(password);
    await page.GetByRole(AriaRole.Button, new() { Name = "Entrar", Exact = true }).ClickAsync();
    await Expect(page.Locator("#orders-table tbody tr")).ToHaveCountAsync(10);
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(artifacts, "orders.png"), FullPage = true });
    Console.WriteLine("Login e carregamento dos 10.000 pedidos: OK.");

    var customerName = $"QA-{Guid.NewGuid():N}";
    await page.Locator("#create-order").ClickAsync();
    await page.Locator("#save-order").ClickAsync();
    await Expect(page.Locator("#order-modal")).ToBeVisibleAsync();
    await Expect(page.Locator("[data-valmsg-for='CustomerName']")).Not.ToBeEmptyAsync();
    await page.Locator("#CustomerName").FillAsync(customerName);
    await page.Locator("#Description").FillAsync("Verificação pelo navegador");
    await page.Locator("#TotalAmount").FillAsync("");
    await page.Locator("#TotalAmount").ClickAsync();
    await page.Locator("#TotalAmount").PressAsync("ControlOrMeta+A");
    await page.Locator("#TotalAmount").PressSequentiallyAsync("1000000,00", new() { Delay = 30 });
    var maximumAmount = await page.EvaluateAsync<decimal>("Number(document.getElementById('TotalAmount').inputmask.unmaskedvalue())");
    Require(maximumAmount == 1000000m, $"A máscara deve aceitar exatamente R$ 1.000.000,00; valor recebido: {maximumAmount}; texto: {await page.Locator("#TotalAmount").InputValueAsync()}.");
    await page.Locator("#TotalAmount").FillAsync("");
    await page.Locator("#TotalAmount").ClickAsync();
    await page.Locator("#TotalAmount").PressAsync("ControlOrMeta+A");
    await page.Locator("#TotalAmount").PressSequentiallyAsync("65046540965,00", new() { Delay = 30 });
    var oversizedAmount = await page.EvaluateAsync<decimal>("Number(document.getElementById('TotalAmount').inputmask.unmaskedvalue())");
    Require(oversizedAmount <= 1000000m, "A máscara deve impedir valores acima de R$ 1.000.000,00.");
    await page.Locator("#TotalAmount").FillAsync("");
    await page.Locator("#TotalAmount").ClickAsync();
    await page.Locator("#TotalAmount").PressAsync("ControlOrMeta+A");
    await page.Locator("#TotalAmount").PressSequentiallyAsync("1234,56", new() { Delay = 30 });
    var amount = await page.EvaluateAsync<decimal>("Number(document.getElementById('TotalAmount').inputmask.unmaskedvalue())");
    Require(amount == 1234.56m, $"A máscara retornou {amount} em vez do valor esperado.");
    var createResponse = await page.RunAndWaitForResponseAsync(() => page.Locator("#save-order").ClickAsync(), response => response.Url.EndsWith("/api/pedidos") && response.Request.Method == "POST");
    Require(createResponse.Status == 201, $"Criação retornou HTTP {createResponse.Status}: {await createResponse.TextAsync()}");
    var order = JsonDocument.Parse(await createResponse.TextAsync()).RootElement.Clone();
    var orderId = order.GetProperty("id").GetInt32();
    await Expect(page.Locator("#order-modal")).Not.ToBeVisibleAsync();
    await page.Locator("#order-search").FillAsync(customerName);
    await Expect(page.Locator("#orders-table tbody tr")).ToHaveCountAsync(1);
    await Expect(page.Locator("#orders-table tbody")).ToContainTextAsync("R$ 1.234,56");
    Console.WriteLine("Validação frontend, máscara monetária, criação e pesquisa: OK.");

    await page.Locator("#orders-table .edit").ClickAsync();
    await Expect(page.Locator("#order-modal")).ToBeVisibleAsync();
    await page.Locator("#Status").SelectOptionAsync("Paid");
    var updateResponse = await page.RunAndWaitForResponseAsync(() => page.Locator("#save-order").ClickAsync(), response => response.Request.Method == "PUT");
    Require(updateResponse.Status == 200, "A edição deve retornar HTTP 200.");
    await Expect(page.Locator("#order-modal")).Not.ToBeVisibleAsync();
    await Expect(page.Locator("#orders-table tbody")).ToContainTextAsync("Pago");

    var apiLogin = await httpClient.PostAsJsonAsync("/api/auth/login", new { username, password });
    apiLogin.EnsureSuccessStatusCode();
    var apiToken = await apiLogin.Content.ReadFromJsonAsync<JsonElement>();
    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiToken.GetProperty("accessToken").GetString());
    await page.Locator("#orders-table .edit").ClickAsync();
    await Expect(page.Locator("#order-modal")).ToBeVisibleAsync();
    var serverUpdate = await httpClient.PutAsJsonAsync($"/api/pedidos/{orderId}", new { customerName, description = "Alterado por outra requisição", totalAmount = 1234.56, status = "Canceled", version = 2 });
    serverUpdate.EnsureSuccessStatusCode();
    var conflictResponse = await page.RunAndWaitForResponseAsync(() => page.Locator("#save-order").ClickAsync(), response => response.Request.Method == "PUT");
    Require(conflictResponse.Status == 409, "A edição de uma versão antiga deve retornar HTTP 409.");
    await Expect(page.Locator("#form-message")).ToContainTextAsync("outra requisição");
    await page.Locator("#order-modal [data-bs-dismiss='modal']").First.ClickAsync();
    Console.WriteLine("Edição e mensagem de conflito entre duas requisições: OK.");

    await page.Locator("#order-search").FillAsync("");
    await Expect(page.Locator("#orders-table tbody tr")).ToHaveCountAsync(10);
    await page.Locator(".dt-paging").GetByText("Próxima", new() { Exact = true }).ClickAsync();
    await page.WaitForFunctionAsync("$('#orders-table').DataTable().page() === 1");
    await page.Locator("#create-order").ClickAsync();
    await Expect(page.Locator("#order-modal")).ToBeVisibleAsync();
    await page.Locator("#CustomerName").FillAsync("Texto que deve permanecer");
    Console.WriteLine("Verificando atualização automática de 10 segundos...");
    await page.WaitForResponseAsync(response => response.Url.Contains("/api/pedidos/datatable") && response.Request.Method == "GET", new PageWaitForResponseOptions { Timeout = 15_000 });
    Require(await page.EvaluateAsync<int>("$('#orders-table').DataTable().page()") == 1, "A atualização automática deve manter a página.");
    await Expect(page.Locator("#CustomerName")).ToHaveValueAsync("Texto que deve permanecer");
    await page.Locator("#order-modal [data-bs-dismiss='modal']").First.ClickAsync();
    Console.WriteLine("Atualização automática preserva página e formulário: OK.");

    await page.Locator("#order-search").FillAsync(customerName);
    await Expect(page.Locator("#orders-table tbody tr")).ToHaveCountAsync(1);
    await Expect(page.Locator("#orders-table tbody")).ToContainTextAsync("Cancelado");
    await page.Locator("#orders-table .delete").ClickAsync();
    var deleteResponse = await page.RunAndWaitForResponseAsync(() => page.Locator("#confirm-delete").ClickAsync(), response => response.Request.Method == "DELETE");
    Require(deleteResponse.Status == 204, "A exclusão deve retornar HTTP 204.");
    await Expect(page.Locator("#delete-modal")).Not.ToBeVisibleAsync();
    await Expect(page.Locator("#orders-table tbody")).ToContainTextAsync("Nenhum pedido encontrado");
    Console.WriteLine("Confirmação e exclusão: OK.");

    await page.Locator("#order-search").FillAsync("");
    await Expect(page.Locator("#orders-table tbody tr")).ToHaveCountAsync(10);
    await page.SetViewportSizeAsync(390, 844);
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(artifacts, "orders-mobile.png"), FullPage = true });
    Require(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= window.innerWidth"), "O layout mobile não deve extrapolar a largura da tela.");
    await page.GetByRole(AriaRole.Button, new() { Name = "Sair", Exact = true }).ClickAsync();
    await Expect(page.Locator("#login-form")).ToBeVisibleAsync();
    Require(javascriptErrors.Count == 0, $"Erros JavaScript: {string.Join(" | ", javascriptErrors)}");
    Console.WriteLine("Layout mobile e logout: OK. Nenhum erro JavaScript.");
    Console.WriteLine($"Capturas de tela em {artifacts}");
}
catch
{
    if (page is not null && !page.IsClosed)
    {
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(artifacts, "failure.png"), FullPage = true });
    }

    throw;
}
finally
{
    if (browser is not null)
    {
        await browser.CloseAsync();
    }

    playwright?.Dispose();

    if (!server.HasExited)
    {
        server.Kill(entireProcessTree: true);
    }

    await server.WaitForExitAsync();
    var errors = await serverErrors;

    if (!string.IsNullOrWhiteSpace(errors))
    {
        Console.Error.WriteLine(errors);
    }
}

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static string FindProjectRoot()
{
    var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

    while (directory is not null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "PedidosCrud.sln")))
        {
            return directory.FullName;
        }

        directory = directory.Parent;
    }

    throw new DirectoryNotFoundException("Execute a verificação a partir da pasta do projeto.");
}

static int GetAvailablePort()
{
    using var listener = new TcpListener(IPAddress.Loopback, 0);
    listener.Start();
    return ((IPEndPoint)listener.LocalEndpoint).Port;
}

static string FindChrome()
{
    string[] paths = [Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe"), "/usr/bin/google-chrome", "/usr/bin/chromium"];
    return paths.FirstOrDefault(File.Exists) ?? throw new FileNotFoundException("Configure BROWSER_EXECUTABLE com o caminho do Chrome ou Chromium instalado.");
}

static async Task WaitForServer(HttpClient client, Process server)
{
    for (var attempt = 0; attempt < 100; attempt++)
    {
        if (server.HasExited)
        {
            throw new InvalidOperationException("O servidor terminou antes de ficar disponível.");
        }

        try
        {
            if ((await client.GetAsync("/health")).IsSuccessStatusCode)
            {
                return;
            }
        }
        catch (HttpRequestException)
        {
        }

        await Task.Delay(200);
    }

    throw new TimeoutException("O servidor não ficou disponível em 20 segundos.");
}
