using Elcop.TI.Application.Common;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elcop.TI.Infrastructure.Identity;

/// <summary>Dados extraídos de um ID token válido do Firebase.</summary>
/// <param name="Uid">Identificador da conta no Firebase.</param>
/// <param name="Email">E-mail verificado da conta.</param>
/// <param name="Nome">Nome de exibição, quando o provedor informa.</param>
public sealed record IdentidadeFirebase(string Uid, string Email, string? Nome);

public interface IFirebaseAutenticacaoService
{
    /// <summary>
    /// Verifica a assinatura e a validade do ID token emitido pelo Firebase.
    /// Devolve <c>null</c> quando o token é inválido, expirou ou não traz e-mail verificado.
    /// </summary>
    Task<IdentidadeFirebase?> ValidarTokenAsync(string idToken, CancellationToken ct = default);
}

/// <summary>
/// Valida ID tokens do Firebase pelo Admin SDK. O Firebase apenas prova <em>quem é</em>
/// a pessoa — os perfis de acesso, a auditoria e o bloqueio de conta continuam no
/// ASP.NET Identity, então nenhuma regra de autorização depende da nuvem.
/// </summary>
public sealed class FirebaseAutenticacaoService : IFirebaseAutenticacaoService
{
    private static readonly object Trava = new();
    private static FirebaseApp? _app;

    private readonly ElcopFirebaseOptions _opcoes;
    private readonly ILogger<FirebaseAutenticacaoService> _logger;

    public FirebaseAutenticacaoService(
        IOptions<ElcopFirebaseOptions> opcoes, ILogger<FirebaseAutenticacaoService> logger)
    {
        _opcoes = opcoes.Value;
        _logger = logger;
    }

    public async Task<IdentidadeFirebase?> ValidarTokenAsync(string idToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(idToken)) return null;

        try
        {
            var app = ObterApp();
            var token = await FirebaseAuth.GetAuth(app).VerifyIdTokenAsync(idToken, ct);

            token.Claims.TryGetValue("email", out var email);
            token.Claims.TryGetValue("email_verified", out var emailVerificado);
            token.Claims.TryGetValue("name", out var nome);

            var endereco = email?.ToString();

            if (string.IsNullOrWhiteSpace(endereco))
            {
                _logger.LogWarning("Token do Firebase sem e-mail (uid {Uid}).", token.Uid);
                return null;
            }

            // Sem e-mail verificado qualquer um poderia cadastrar o endereço de outra pessoa.
            if (emailVerificado is not true)
            {
                _logger.LogWarning("Login recusado: e-mail {Email} não verificado no Firebase.", endereco);
                return null;
            }

            return new IdentidadeFirebase(token.Uid, endereco, nome?.ToString());
        }
        catch (FirebaseAuthException ex)
        {
            _logger.LogWarning(ex, "ID token do Firebase recusado.");
            return null;
        }
    }

    /// <summary>
    /// O FirebaseApp é um singleton do processo — inicializa uma vez e reaproveita.
    /// Sem <c>CaminhoCredencial</c> usa as Application Default Credentials, que é o
    /// caminho no Cloud Run (identidade do ambiente, sem arquivo de chave no disco).
    /// </summary>
    private FirebaseApp ObterApp()
    {
        if (_app is not null) return _app;

        lock (Trava)
        {
            if (_app is not null) return _app;

            _app = FirebaseApp.DefaultInstance ?? FirebaseApp.Create(new AppOptions
            {
                ProjectId = _opcoes.ProjectId,
                Credential = CredencialGoogle.Resolver(_opcoes.CaminhoCredencial)
            });

            _logger.LogInformation("Firebase inicializado para o projeto {ProjectId}.", _opcoes.ProjectId);
            return _app;
        }
    }
}
