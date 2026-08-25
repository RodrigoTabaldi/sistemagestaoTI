using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elcop.TI.Web.Tests;

/// <summary>
/// Autenticação falsa para os testes de integração: lê o perfil e o e-mail de cabeçalhos
/// HTTP em vez de exigir um login real por senha/cookie do Identity. Isso mantém os testes
/// de autorização focados no que eles precisam provar — que as políticas de
/// <see cref="Elcop.TI.Web.Infra.Politicas"/> realmente barram quem não tem o perfil — sem
/// depender de criar usuário, confirmar e-mail e autenticar por senha a cada teste.
/// </summary>
public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string Esquema = "TesteElcop";
    public const string CabecalhoPerfil = "X-Teste-Perfil";
    public const string CabecalhoEmail = "X-Teste-Email";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(CabecalhoPerfil, out var perfil) || perfil.Count == 0)
            return Task.FromResult(AuthenticateResult.NoResult());

        var email = Request.Headers.TryGetValue(CabecalhoEmail, out var valor) && valor.Count > 0
            ? valor.ToString()
            : "teste@elcop.com.br";

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, email),
            new(ClaimTypes.Name, email),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.GivenName, "Usuário de Teste"),
            new(ClaimTypes.Role, perfil.ToString()),
        };

        var identidade = new ClaimsIdentity(claims, Esquema);
        var principal = new ClaimsPrincipal(identidade);
        var ticket = new AuthenticationTicket(principal, Esquema);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
