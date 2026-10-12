using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PedidosCrud.Web.Security;

public sealed class CookieAntiforgeryFilter(IAntiforgery antiforgery) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var request = context.HttpContext.Request;
        var isSafeMethod = HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method) || HttpMethods.IsOptions(request.Method);
        var usesBearerToken = request.Headers.Authorization.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);

        if (isSafeMethod || !request.Path.StartsWithSegments("/api") || usesBearerToken || !request.Cookies.ContainsKey(AuthOptions.CookieName))
        {
            return;
        }

        if (!await antiforgery.IsRequestValidAsync(context.HttpContext))
        {
            context.Result = new ObjectResult(new ProblemDetails { Status = 400, Title = "Sessão da página inválida", Detail = "Atualize a página e tente novamente." }) { StatusCode = 400 };
        }
    }
}
