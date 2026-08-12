namespace Elcop.TI.Application.Common;

/// <summary>
/// Autocadastro pela tela de login, lido da seção <c>Elcop:AutoCadastro</c>.
/// </summary>
public sealed class ElcopAutoCadastroOptions
{
    public const string Secao = "Elcop:AutoCadastro";

    /// <summary>Exibe (ou não) o link "Criar conta" na tela de login.</summary>
    public bool Habilitado { get; set; } = true;

    /// <summary>
    /// Perfil aplicado a quem se cadastra. <c>Consulta</c> é somente leitura —
    /// promover a Técnico ou Administrador é decisão do administrador, em Usuários.
    /// </summary>
    public string PerfilPadrao { get; set; } = "Consulta";

    /// <summary>
    /// Com <c>false</c> (padrão), a conta nasce desabilitada e só entra depois que um
    /// administrador a libera em Usuários. Ligar isso dá acesso imediato a qualquer
    /// pessoa que consiga abrir a tela de login — só faça em rede interna confiável.
    /// </summary>
    public bool AprovacaoAutomatica { get; set; }

    /// <summary>
    /// Restringe o cadastro a estes domínios de e-mail (ex.: <c>elcop.com.br</c>).
    /// Vazio aceita qualquer domínio.
    /// </summary>
    public string[] DominiosPermitidos { get; set; } = [];

    /// <summary>Verifica se o e-mail pertence a um dos domínios permitidos.</summary>
    public bool DominioAceito(string email)
    {
        if (DominiosPermitidos.Length == 0) return true;

        var arroba = email.LastIndexOf('@');
        if (arroba < 0) return false;

        var dominio = email[(arroba + 1)..];

        return DominiosPermitidos.Any(d =>
            dominio.Equals(d.TrimStart('@'), StringComparison.OrdinalIgnoreCase));
    }
}
