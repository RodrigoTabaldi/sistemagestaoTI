using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Elcop.TI.Infrastructure.Identity;

/// <summary>
/// Acrescenta o nome de exibição e o cargo ao cookie de autenticação, evitando
/// uma consulta ao banco a cada renderização do cabeçalho.
/// </summary>
public class UsuarioClaimsFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
{
    public UsuarioClaimsFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> options)
        : base(userManager, roleManager, options)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        identity.AddClaim(new Claim(ClaimTypes.GivenName, user.NomeCompleto));

        if (!string.IsNullOrWhiteSpace(user.Cargo))
            identity.AddClaim(new Claim("cargo", user.Cargo));

        identity.AddClaim(new Claim("iniciais", user.Iniciais));

        return identity;
    }
}
