namespace Elcop.TI.Application.Common;

/// <summary>
/// Configuração da integração com o Firebase, lida da seção <c>Elcop:Firebase</c>.
///
/// Enquanto <see cref="Habilitado"/> for <c>false</c> (padrão) nada do Firebase é
/// inicializado: o login continua sendo só e-mail/senha e o armazenamento continua local.
/// </summary>
public sealed class ElcopFirebaseOptions
{
    public const string Secao = "Elcop:Firebase";

    /// <summary>Liga o login via Firebase e o armazenamento no Cloud Storage.</summary>
    public bool Habilitado { get; set; }

    /// <summary>ID do projeto no console do Firebase (ex.: <c>elcop-ti</c>).</summary>
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>
    /// Caminho do JSON da conta de serviço (Firebase → Configurações → Contas de serviço).
    /// Vazio faz o SDK usar as Application Default Credentials — é o caminho no Cloud Run,
    /// onde a identidade já vem do ambiente e não há arquivo de chave para guardar.
    /// </summary>
    public string CaminhoCredencial { get; set; } = string.Empty;

    /// <summary>Chave da API web (Firebase → Configurações → Seus apps → App da Web).</summary>
    public string ApiKeyWeb { get; set; } = string.Empty;

    /// <summary>Domínio de autenticação, normalmente <c>PROJETO.firebaseapp.com</c>.</summary>
    public string AuthDomain { get; set; } = string.Empty;

    /// <summary>Bucket do Cloud Storage, normalmente <c>PROJETO.appspot.com</c>.</summary>
    public string Bucket { get; set; } = string.Empty;

    /// <summary>
    /// Cria automaticamente o usuário local no primeiro login via Firebase.
    /// Deixe <c>false</c> para que só quem já foi cadastrado em Usuários consiga entrar —
    /// caso contrário qualquer conta Google aceita pelo projeto vira usuário do sistema.
    /// </summary>
    public bool ProvisionarAutomaticamente { get; set; }

    /// <summary>Perfil aplicado aos usuários provisionados automaticamente.</summary>
    public string PerfilPadrao { get; set; } = "Consulta";

    /// <summary>O login via Firebase só aparece quando há projeto e chave web configurados.</summary>
    public bool LoginDisponivel =>
        Habilitado
        && !string.IsNullOrWhiteSpace(ProjectId)
        && !string.IsNullOrWhiteSpace(ApiKeyWeb);
}
