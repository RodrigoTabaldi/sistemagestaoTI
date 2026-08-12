using System.Security.Claims;
using Elcop.TI.Application.Common;

namespace Elcop.TI.Web.Services;

/// <summary>
/// Expõe a identidade da requisição HTTP para as camadas internas, sem que elas
/// precisem conhecer <c>HttpContext</c>.
/// </summary>
public class UsuarioAtual : IUsuarioAtual
{
    private readonly IHttpContextAccessor _acessor;

    public UsuarioAtual(IHttpContextAccessor acessor) => _acessor = acessor;

    private ClaimsPrincipal? Principal => _acessor.HttpContext?.User;

    public string? NomeUsuario => Principal?.Identity?.Name;

    public string? NomeExibicao =>
        Principal?.FindFirst(ClaimTypes.GivenName)?.Value
        ?? Principal?.Identity?.Name
        ?? "sistema";

    public string? EnderecoIp => _acessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public bool EstaAutenticado => Principal?.Identity?.IsAuthenticated ?? false;

    public bool PossuiPerfil(string perfil) => Principal?.IsInRole(perfil) ?? false;
}
