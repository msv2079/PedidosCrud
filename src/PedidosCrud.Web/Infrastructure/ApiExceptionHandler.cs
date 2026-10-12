using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PedidosCrud.Model;

namespace PedidosCrud.Web.Infrastructure;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var statusCode = exception switch { InputValidationException => 400, OrderNotFoundException => 404, OrderConflictException => 409, _ => 500 };
        ProblemDetails problem = exception is InputValidationException validation ? new ValidationProblemDetails(validation.Errors) : new ProblemDetails();
        problem.Status = statusCode;
        problem.Title = statusCode switch { 400 => "Dados inválidos", 404 => "Pedido não encontrado", 409 => "Conflito de alteração", _ => "Não foi possível concluir a operação" };
        problem.Detail = statusCode == 500 ? "Ocorreu um erro inesperado. Tente novamente e informe o código da requisição se o problema persistir." : exception.Message;
        problem.Extensions["traceId"] = context.TraceIdentifier;
        context.Response.StatusCode = statusCode;

        if (statusCode == 500)
        {
            logger.LogError(exception, "Falha na requisição {TraceId}: {Method} {Path}.", context.TraceIdentifier, context.Request.Method, context.Request.Path);
        }

        await context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json", cancellationToken: cancellationToken);
        return true;
    }
}
