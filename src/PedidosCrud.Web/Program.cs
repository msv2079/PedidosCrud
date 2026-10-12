using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Tokens;
using PedidosCrud.Repository;
using PedidosCrud.Service;
using PedidosCrud.Web.Infrastructure;
using PedidosCrud.Web.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Services.AddSingleton<ILoggerProvider, Log4NetLoggerProvider>();
builder.Services.AddSingleton(provider =>
{
    var options = provider.GetRequiredService<IConfiguration>().GetSection("Authentication").Get<AuthOptions>() ?? new AuthOptions();
    options.Validate();
    return options;
});
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<OrderSeedGenerator>();
builder.Services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddScoped<CookieAntiforgeryFilter>();
builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddControllersWithViews(options => options.Filters.AddService<CookieAntiforgeryFilter>()).AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme).Configure<AuthOptions>((options, authOptions) =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = authOptions.Issuer,
        ValidAudience = authOptions.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authOptions.SigningKey)),
        NameClaimType = "name",
        ClockSkew = TimeSpan.FromSeconds(15)
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (!context.Request.Headers.ContainsKey("Authorization"))
            {
                context.Token = context.Request.Cookies[AuthOptions.CookieName];
            }

            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            context.HandleResponse();

            if (context.Request.Path.StartsWithSegments("/api"))
            {
                return Results.Problem(statusCode: 401, title: "Autenticação necessária", detail: "Entre novamente para continuar.").ExecuteAsync(context.HttpContext);
            }

            context.Response.Redirect("/account/login");
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.OnRejected = (context, cancellationToken) => new ValueTask(Results.Problem(statusCode: 429, title: "Muitas tentativas", detail: "Aguarde um minuto e tente novamente.").ExecuteAsync(context.HttpContext));
});

var app = builder.Build();
app.Services.GetRequiredService<AuthOptions>();
app.UseExceptionHandler();
app.UseRequestLocalization(new RequestLocalizationOptions().SetDefaultCulture("pt-BR").AddSupportedCultures("pt-BR").AddSupportedUICultures("pt-BR"));
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "same-origin";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self'; connect-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
    await next(context);
});
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();
app.MapControllerRoute("default", "{controller=Orders}/{action=Index}/{id?}");
app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).AllowAnonymous();

var repository = app.Services.GetRequiredService<IOrderRepository>();
app.Logger.LogInformation("Aplicação iniciada com {OrderCount} pedidos em memória.", repository.GetSnapshot().Count);
app.Run();

public partial class Program;
