using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using PedidosCrud.Web.Models;

namespace PedidosCrud.Web.Security;

public sealed record TokenResult(string AccessToken, DateTimeOffset ExpiresAt, string TokenType = "Bearer");

public sealed class TokenService(AuthOptions options, TimeProvider timeProvider)
{
    public bool ValidateCredentials(LoginInput input)
    {
        var usernameMatches = FixedTimeEquals(input.Username, options.Username);
        var passwordMatches = FixedTimeEquals(input.Password, options.Password);
        return usernameMatches & passwordMatches;
    }

    public TokenResult CreateToken()
    {
        var now = timeProvider.GetUtcNow();
        var expiresAt = now.AddMinutes(options.TokenMinutes);
        Claim[] claims = [new("sub", options.Username), new("name", options.Username), new("jti", Guid.NewGuid().ToString("N"))];
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(options.Issuer, options.Audience, claims, now.UtcDateTime, expiresAt.UtcDateTime, credentials);
        return new TokenResult(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    private static bool FixedTimeEquals(string? received, string expected)
    {
        var receivedHash = SHA256.HashData(Encoding.UTF8.GetBytes(received ?? string.Empty));
        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expected));
        return CryptographicOperations.FixedTimeEquals(receivedHash, expectedHash);
    }
}
