namespace PedidosCrud.Web.Security;

public sealed class AuthOptions
{
    public const string CookieName = "PedidosCrud.AccessToken";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string SigningKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "PedidosCrud";
    public string Audience { get; set; } = "PedidosCrud.Client";
    public int TokenMinutes { get; set; } = 30;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Username) || Username.Length > 100 || Password.Length is < 12 or > 256 || System.Text.Encoding.UTF8.GetByteCount(SigningKey) < 32)
        {
            throw new InvalidOperationException("Configure Authentication__Username, Authentication__Password (mínimo 12 caracteres) e Authentication__SigningKey (mínimo 32 bytes). Consulte o README ou execute scripts/Start-Local.ps1.");
        }

        if (TokenMinutes is < 1 or > 120 || string.IsNullOrWhiteSpace(Issuer) || string.IsNullOrWhiteSpace(Audience))
        {
            throw new InvalidOperationException("A configuração de expiração, issuer ou audience do JWT é inválida.");
        }
    }
}
