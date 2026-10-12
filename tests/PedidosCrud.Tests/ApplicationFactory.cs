using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace PedidosCrud.Tests;

public sealed class ApplicationFactory : WebApplicationFactory<Program>
{
    public string Username { get; } = $"test-{Guid.NewGuid():N}";
    public string Password { get; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
    public string SigningKey { get; } = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((context, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:Username"] = Username,
            ["Authentication:Password"] = Password,
            ["Authentication:SigningKey"] = SigningKey
        }));
    }
}
