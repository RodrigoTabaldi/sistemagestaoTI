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

if (!builder.Environment.IsDevelopment())
{
    var hostsPermitidos = builder.Configuration["AllowedHosts"];
    if (string.IsNullOrWhiteSpace(hostsPermitidos) || hostsPermitidos.Split(';').Any(h => h.Trim() == "*"))
    {
        throw new InvalidOperationException(
            "AllowedHosts deve listar hosts explícitos fora de Development; não use '*'.");
    }
}

// ----------------------------------------------------------------- Serviços

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioAtual, UsuarioAtual>();

builder.Services.AdicionarInfraestrutura(
    builder.Configuration, habilitarErrosDetalhados: builder.Environment.IsDevelopment());
builder.Services.AdicionarCamadaDeAplicacao();
builder.Services.AddScoped<ISelecaoService, SelecaoService>();

var mvc = builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<TratamentoDeRegraDeNegocioFilter>();
})
.AddViewOptions(options => options.HtmlHelperOptions.ClientValidationEnabled = true);

if (builder.Environment.IsDevelopment())
    mvc.AddRazorRuntimeCompilation();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("banco-de-dados");

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

    // Abrir demanda é de todo mundo: o usuário comum registra o chamado e a TI conduz o
    // atendimento. Exigir um perfil conhecido (e não apenas autenticação) mantém a regra
    // explícita — conta sem perfil atribuído continua sem poder escrever nada.
    options.AddPolicy(Politicas.AbrirDemanda, p => p.RequireRole(Perfis.Todos));

    // Cadastrar colaborador/ativo também é de todo mundo, pelo mesmo motivo: qualquer
    // perfil conhecido pode criar o registro, mas editar ou excluir o que já existe
    // continua exigindo Operar.
    options.AddPolicy(Politicas.Cadastrar, p => p.RequireRole(Perfis.Todos));
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

// Sem APM externo no projeto: isto é o mínimo de instrumentação de performance possível
// sem adicionar dependência nova — loga só o que foge do normal, para não afogar o log em
// produção (onde "Microsoft.AspNetCore" fica em Warning e esconde o timing padrão do
// próprio framework).
app.Use(async (context, next) =>
{
    var cronometro = System.Diagnostics.Stopwatch.StartNew();
    try
    {
        await next();
    }
    finally
    {
        cronometro.Stop();
        TelemetriaAplicacao.RegistrarRequisicao(
            context.Request.Method, context.Response.StatusCode, cronometro.Elapsed);

        if (cronometro.ElapsedMilliseconds > 1000)
        {
            app.Logger.LogWarning(
                "Requisição lenta: {Metodo} {Caminho} levou {DuracaoMs} ms (status {Status}).",
                context.Request.Method, context.Request.Path, cronometro.ElapsedMilliseconds,
                context.Response.StatusCode);
        }
    }
});

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

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
}).AllowAnonymous();

var readiness = app.MapHealthChecks("/health/ready");
if (app.Environment.IsDevelopment())
    readiness.AllowAnonymous();
else
    readiness.RequireAuthorization(Politicas.Administrar);

// ----------------------------------------------------------------- Inicialização
await DbInitializer.InicializarAsync(app.Services);

app.Run();

// Marcador público: permite que WebApplicationFactory<Program> (testes de integração
// da camada Web) enxergue este tipo de fora do assembly — top-level statements geram
// uma classe Program implicitamente internal.
public partial class Program;
