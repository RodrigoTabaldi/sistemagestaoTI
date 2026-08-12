namespace Elcop.TI.Application.Common;

/// <summary>
/// Identidade do usuário autenticado, injetada pela camada Web para carimbar
/// auditoria e campos de criação/alteração sem acoplar a Application ao HttpContext.
/// </summary>
public interface IUsuarioAtual
{
    string? NomeUsuario { get; }

    string? NomeExibicao { get; }

    string? EnderecoIp { get; }

    bool EstaAutenticado { get; }

    bool PossuiPerfil(string perfil);
}

/// <summary>Fallback usado em processos sem requisição HTTP (seed, jobs).</summary>
public sealed class UsuarioSistema : IUsuarioAtual
{
    public string? NomeUsuario => "sistema";

    public string? NomeExibicao => "Sistema";

    public string? EnderecoIp => null;

    public bool EstaAutenticado => false;

    public bool PossuiPerfil(string perfil) => true;
}
