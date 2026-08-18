using System.Security.Cryptography;
using Elcop.TI.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Elcop.TI.Infrastructure.Persistence;

/// <summary>
/// Aplica as migrations pendentes, cria os perfis de acesso e garante que exista
/// um administrador para o primeiro login. O banco nasce vazio: todo o conteúdo
/// é cadastrado pelo próprio usuário.
/// </summary>
public static class DbInitializer
{
    /// <summary>Senha publicada em versões anteriores — se ainda estiver em uso, o log avisa.</summary>
    private const string SenhaLegada = "Elcop@2026";

    public static async Task InicializarAsync(IServiceProvider provider, CancellationToken ct = default)
    {
        using var escopo = provider.CreateScope();
        var servicos = escopo.ServiceProvider;

        var logger = servicos.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");
        var db = servicos.GetRequiredService<AppDbContext>();
        var config = servicos.GetRequiredService<IConfiguration>();
        var ambiente = servicos.GetRequiredService<IHostEnvironment>();

        await db.Database.MigrateAsync(ct);
        logger.LogInformation("Banco de dados atualizado.");

        await CriarPerfisAsync(servicos.GetRequiredService<RoleManager<IdentityRole>>(), ct);
        await CriarAdministradorAsync(
            servicos.GetRequiredService<UserManager<ApplicationUser>>(), config, logger, ambiente);
    }

    private static async Task CriarPerfisAsync(RoleManager<IdentityRole> roles, CancellationToken ct)
    {
        foreach (var perfil in Perfis.Todos)
        {
            if (!await roles.RoleExistsAsync(perfil))
                await roles.CreateAsync(new IdentityRole(perfil));
        }
    }

    /// <summary>
    /// Cria o administrador inicial na primeira execução. A senha vem da configuração
    /// (user-secrets ou variável de ambiente <c>Elcop__Admin__Senha</c>); se não houver
    /// nenhuma, uma senha aleatória é gerada e registrada no log uma única vez.
    /// </summary>
    private static async Task CriarAdministradorAsync(
        UserManager<ApplicationUser> users, IConfiguration config, ILogger logger, IHostEnvironment ambiente)
    {
        var email = config["Elcop:Admin:Email"] ?? "admin@elcop.com.br";

        if (await users.FindByEmailAsync(email) is not null)
        {
            // O administrador já existe: nada a fazer e nenhuma senha a expor.
            return;
        }

        var senhaConfigurada = config["Elcop:Admin:Senha"];

        // Fora de Development, gerar e logar uma senha é um risco desnecessário — se o log for
        // centralizado, a senha fica retida ali indefinidamente. Em produção é melhor falhar
        // rápido e pedir a senha explicitamente do que criar uma conta cuja senha já vazou pro log.
        if (string.IsNullOrWhiteSpace(senhaConfigurada) && !ambiente.IsDevelopment())
        {
            throw new InvalidOperationException(
                "Elcop:Admin:Senha não foi configurada. Defina-a via user-secrets ou a variável de " +
                "ambiente Elcop__Admin__Senha antes de subir o sistema fora de Development — " +
                "fora de Development o administrador inicial não é criado com senha gerada e logada.");
        }

        var senha = string.IsNullOrWhiteSpace(senhaConfigurada) ? GerarSenhaAleatoria() : senhaConfigurada;

        var admin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            NomeCompleto = "Administrador",
            Cargo = "Coordenação de TI"
        };

        var resultado = await users.CreateAsync(admin, senha);

        if (!resultado.Succeeded)
        {
            logger.LogError("Falha ao criar o administrador inicial: {Erros}",
                string.Join("; ", resultado.Errors.Select(e => e.Description)));
            return;
        }

        await users.AddToRoleAsync(admin, Perfis.Administrador);

        if (string.IsNullOrWhiteSpace(senhaConfigurada))
        {
            logger.LogWarning(
                "Administrador criado: {Email} · senha inicial: {Senha}\n" +
                "Esta senha aparece no log apenas nesta execução. Entre no sistema e troque-a em Conta → Perfil.",
                email, senha);
        }
        else
        {
            logger.LogInformation("Administrador criado com a senha definida em Elcop:Admin:Senha ({Email}).", email);

            if (senhaConfigurada == SenhaLegada)
            {
                logger.LogWarning(
                    "A senha configurada é a senha de exemplo das versões anteriores. " +
                    "Defina outra em Elcop:Admin:Senha (user-secrets ou Elcop__Admin__Senha) e troque-a no sistema.");
            }
        }
    }

    /// <summary>
    /// Senha aleatória que satisfaz a política do Identity (8+, maiúscula, minúscula, dígito
    /// e caractere especial).
    /// </summary>
    private static string GerarSenhaAleatoria()
    {
        const string maiusculas = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string minusculas = "abcdefghijkmnopqrstuvwxyz";
        const string digitos = "23456789";
        const string especiais = "!@#$%&*+=?";
        const string todos = maiusculas + minusculas + digitos + especiais;

        var caracteres = new List<char>
        {
            maiusculas[RandomNumberGenerator.GetInt32(maiusculas.Length)],
            minusculas[RandomNumberGenerator.GetInt32(minusculas.Length)],
            digitos[RandomNumberGenerator.GetInt32(digitos.Length)],
            especiais[RandomNumberGenerator.GetInt32(especiais.Length)]
        };

        while (caracteres.Count < 14)
            caracteres.Add(todos[RandomNumberGenerator.GetInt32(todos.Length)]);

        // Embaralha para os três primeiros caracteres não ficarem sempre no mesmo padrão.
        for (var i = caracteres.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (caracteres[i], caracteres[j]) = (caracteres[j], caracteres[i]);
        }

        return new string(caracteres.ToArray());
    }
}
