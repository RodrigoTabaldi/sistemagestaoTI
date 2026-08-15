using System.Globalization;
using System.Threading.RateLimiting;
using Elcop.TI.Application;
using Elcop.TI.Application.Common;
using Elcop.TI.Infrastructure;
using Elcop.TI.Infrastructure.Identity;
using Elcop.TI.Infrastructure.Persistence;
using Elcop.TI.Web.Infra;
using Elcop.TI.Web.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------------------------------------------- Serviços

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioAtual, UsuarioAtual>();

builder.Services.AdicionarInfraestrutura(builder.Configuration);
builder.Services.AdicionarCamadaDeAplicacao();
builder.Services.AddScoped<ISelecaoService, SelecaoService>();

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<TratamentoDeRegraDeNegocioFilter>();
})
.AddRazorRuntimeCompilation()
.AddViewOptions(options => options.HtmlHelperOptions.ClientValidationEnabled = true);

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Conta/Entrar";
    options.LogoutPath = "/Conta/Sair";
    options.AccessDeniedPath = "/Conta/AcessoNegado";
    options.ExpireTimeSpan = TimeSpan.FromHours(10);
    options.SlidingExpiration = true;
    options.Cookie.Name = "elcop.ti.auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddRateLimiter(options =>
{
    // Política global: 300 requisições por minuto por IP
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 300,
                Window = TimeSpan.FromMinutes(1)
            }));

    // Política "login": 10 tentativas por 5 minutos por IP (spray protection)
    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(5)
            }));

    // Política "upload": 20 requisições por minuto por IP (proteção de upload abuse)
    options.AddPolicy("upload", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1)
            }));

    // Recusando requisições em vez de enfileirá-las: mais simples e apropriado para web apps
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = (context, _) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        return ValueTask.CompletedTask;
    };
});

builder.Services.AddAuthorization(options =>
{
    // Sem autenticação não se navega em lugar nenhum: o opt-out é explícito ([AllowAnonymous]).
    options.FallbackPolicy = options.DefaultPolicy;

    options.AddPolicy(Politicas.Administrar, p => p.RequireRole(Perfis.Administrador));
    options.AddPolicy(Politicas.Operar, p => p.RequireRole(Perfis.Administrador, Perfis.Tecnico));
});

builder.Services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");

builder.Services.AddRequestLocalization(options =>
{
    var ptBr = new CultureInfo("pt-BR");
    options.DefaultRequestCulture = new RequestCulture(ptBr);
    options.SupportedCultures = new List<CultureInfo> { ptBr };
    options.SupportedUICultures = new List<CultureInfo> { ptBr };
});

builder.Services.Configure<RazorViewEngineOptions>(options =>
{
    // Permite organizar partials compartilhadas em /Views/Componentes.
    options.ViewLocationFormats.Add("/Views/Componentes/{0}" + RazorViewEngine.ViewExtension);
});

var app = builder.Build();

// ----------------------------------------------------------------- Pipeline

// Forwarded headers: necessário para funcionar corretamente atrás de load balancer
// (Render, Cloud Run, etc.). Deve vir ANTES de qualquer middleware que use RemoteIpAddress.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    KnownNetworks = { }, // PaaS: proxy não tem IP fixo conhecido
    KnownProxies = { }
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Erro");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseStatusCodePagesWithReExecute("/Erro/{0}");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRequestLocalization();

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Painel}/{action=Index}/{id?}");

app.MapHealthChecks("/health");

// ----------------------------------------------------------------- Inicialização
await DbInitializer.InicializarAsync(app.Services);

app.Run();
