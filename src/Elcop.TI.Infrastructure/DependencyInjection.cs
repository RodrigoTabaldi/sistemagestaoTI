using Elcop.TI.Application.Common;
using Elcop.TI.Infrastructure.Identity;
using Elcop.TI.Infrastructure.Persistence;
using Elcop.TI.Infrastructure.Storage;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elcop.TI.Infrastructure;

/// <summary>
/// Registro do acesso a dados, da identidade e dos serviços de nuvem.
/// Tudo é escolhido por configuração:
///   "Elcop:Provedor"      → Sqlite | SqlServer | Postgres
///   "Elcop:Armazenamento" → Local | Firebase
///   "Elcop:Firebase"      → credenciais e chaves (inerte enquanto Habilitado = false)
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AdicionarInfraestrutura(
        this IServiceCollection services, IConfiguration configuration)
    {
        var provedor = configuration["Elcop:Provedor"] ?? "Sqlite";
        var conexao = configuration.GetConnectionString("Padrao")
            ?? "Data Source=elcop-ti.db";

        var assemblyMigrations = typeof(AppDbContext).Assembly.FullName;

        if (provedor.Equals("Postgres", StringComparison.OrdinalIgnoreCase)
            || provedor.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase))
        {
            // Contexto próprio porque as migrations do Postgres são um conjunto separado
            // (ver AppDbContextPostgres). O restante da aplicação continua pedindo AppDbContext.
            services.AddDbContext<AppDbContextPostgres>(options =>
            {
                options.UseNpgsql(conexao, npg =>
                {
                    npg.MigrationsAssembly(assemblyMigrations);
                    npg.EnableRetryOnFailure(3);
                });

                options.EnableDetailedErrors();
            });

            services.AddScoped<AppDbContext>(sp => sp.GetRequiredService<AppDbContextPostgres>());
        }
        else
        {
            services.AddDbContext<AppDbContext>(options =>
            {
                if (provedor.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
                {
                    options.UseSqlServer(conexao, sql =>
                    {
                        sql.MigrationsAssembly(assemblyMigrations);
                        sql.EnableRetryOnFailure(3);
                    });
                }
                else
                {
                    options.UseSqlite(conexao, sqlite =>
                        sqlite.MigrationsAssembly(assemblyMigrations));
                }

                options.EnableDetailedErrors();
            });
        }

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;

            options.User.RequireUniqueEmail = true;

            // Bloquei,o temporário apósooo tentativdas seguidas de senha incorreta.
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddClaimsPrincipalFactory<UsuarioClaimsFactory>()
        .AddDefaultTokenProviders();

        services.Configure<ElcopAutoCadastroOptions>(
            configuration.GetSection(ElcopAutoCadastroOptions.Secao));

        AdicionarFirebase(services, configuration);
        AdicionarArmazenamento(services, configuration);

        return services;
    }

    private static void AdicionarFirebase(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ElcopFirebaseOptions>(configuration.GetSection(ElcopFirebaseOptions.Secao));

        var opcoes = configuration.GetSection(ElcopFirebaseOptions.Secao).Get<ElcopFirebaseOptions>()
            ?? new ElcopFirebaseOptions();

        // Só registra o serviço quando o Firebase está ligado: com ele ausente do container,
        // o ContaController resolve null e o caminho de login via Firebase simplesmente não existe.
        if (opcoes.Habilitado)
            services.AddSingleton<IFirebaseAutenticacaoService, FirebaseAutenticacaoService>();
    }

    private static void AdicionarArmazenamento(IServiceCollection services, IConfiguration configuration)
    {
        var destino = configuration["Elcop:Armazenamento"] ?? "Local";

        if (destino.Equals("Firebase", StringComparison.OrdinalIgnoreCase))
            services.AddSingleton<IArmazenamentoArquivos, ArmazenamentoFirebase>();
        else
            services.AddSingleton<IArmazenamentoArquivos, ArmazenamentoLocal>();
    }
}
