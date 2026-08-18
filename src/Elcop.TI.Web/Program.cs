using System.Globalization;
using System.Threading.RateLimiting;
using Elcop.TI.Application;
using Elcop.TI.Application.Common;
using Elcop.TI.Infrastructure;
using Elcop.TI.Infrastructure.Identity;
using Elcop.TI.Infrastructure.Persistence;
using Elcop.TI.Web.Infra;
using Elcop.TI.Web.Services;
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
    // Always em produção: o cookie de sessão nunca pode trafegar sem TLS. Em
    // desenvolvimento local (sem HTTPS configurado) SameAsRequest evita travar o login.
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});

// Limite por IP nas rotas de autenticação: o bloqueio de conta do Identity já existe,
// mas é por usuário — sem isto um único IP pode tentar senha contra centenas de e-mails
// diferentes (credential stuffing) ou martelar o autocadastro sem nunca disparar o lockout.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("Autenticacao", limiter =>
    {
        limiter.PermitLimit = 10;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });
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

app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers.XContentTypeOptions = "nosniff";
    headers.XFrameOptions = "DENY";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' https://www.gstatic.com; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data: https://storage.googleapis.com; " +
        "font-src 'self' data:; " +
        "connect-src 'self' https://*.googleapis.com; " +
        "object-src 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'; " +
        "frame-ancestors 'none'";

    await next();
});

app.UseStaticFiles();
app.UseRequestLocalization();

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Painel}/{action=Index}/{id?}");

// ----------------------------------------------------------------- Inicialização
await DbInitializer.InicializarAsync(app.Services);

app.Run();
