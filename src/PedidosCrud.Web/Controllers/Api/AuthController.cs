using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PedidosCrud.Web.Models;
using PedidosCrud.Web.Security;

namespace PedidosCrud.Web.Controllers.Api;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(TokenService tokenService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public ActionResult<TokenResult> Login(LoginInput input)
    {
        if (!tokenService.ValidateCredentials(input))
        {
            return Unauthorized(new ProblemDetails { Status = 401, Title = "Credenciais inválidas", Detail = "Usuário ou senha inválidos." });
        }

        return Ok(tokenService.CreateToken());
    }
}
