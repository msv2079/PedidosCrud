using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PedidosCrud.Web.Models;
using PedidosCrud.Web.Security;

namespace PedidosCrud.Web.Controllers;

[Route("account")]
public sealed class AccountController(TokenService tokenService) : Controller
{
    [AllowAnonymous]
    [HttpGet("login")]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Orders");
        }

        return View(new LoginInput());
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("login")]
    public IActionResult Login(LoginInput input)
    {
        if (!ModelState.IsValid || !tokenService.ValidateCredentials(input))
        {
            ModelState.AddModelError(string.Empty, "Usuário ou senha inválidos.");
            input.Password = string.Empty;
            ModelState.Remove(nameof(input.Password));
            return View(input);
        }

        var token = tokenService.CreateToken();
        Response.Cookies.Append(AuthOptions.CookieName, token.AccessToken, new CookieOptions { HttpOnly = true, Secure = Request.IsHttps, SameSite = SameSiteMode.Strict, Expires = token.ExpiresAt, Path = "/", IsEssential = true });
        return RedirectToAction("Index", "Orders");
    }

    [Authorize]
    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(AuthOptions.CookieName, new CookieOptions { Path = "/" });
        return RedirectToAction(nameof(Login));
    }
}
