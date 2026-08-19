using System.Security.Claims;
using Elcop.TI.Infrastructure.Identity;

namespace Elcop.TI.Web.Infra;

/// <summary>
/// Leitura dos perfis do usuário logado, usada pelas views (e pelos controllers) para
/// mostrar ou esconder ações. Espelha as políticas de <see cref="Politicas"/>: a
/// autorização de verdade continua sendo feita pelo <c>[Authorize]</c>, aqui só se
/// decide o que faz sentido exibir.
/// </summary>
public static class PermissoesDoUsuario
{
    public static bool EhAdministrador(this ClaimsPrincipal usuario) =>
        usuario.IsInRole(Perfis.Administrador);

    /// <summary>Administrador ou técnico: quem conduz o inventário e o atendimento.</summary>
    public static bool PodeOperar(this ClaimsPrincipal usuario) =>
        usuario.IsInRole(Perfis.Administrador) || usuario.IsInRole(Perfis.Tecnico);

    /// <summary>Todo usuário com perfil pode registrar a própria demanda.</summary>
    public static bool PodeAbrirDemanda(this ClaimsPrincipal usuario) =>
        Perfis.Todos.Any(usuario.IsInRole);
}
