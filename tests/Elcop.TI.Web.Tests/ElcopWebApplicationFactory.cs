using System.Data.Common;
using Elcop.TI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elcop.TI.Web.Tests;

/// <summary>
/// Sobe a aplicação inteira (Program.cs, pipeline, DI, DbInitializer incluído) num
/// TestServer, trocando só duas coisas: o banco passa a ser SQLite em memória (isolado por
/// instância da factory, nunca toca o elcop-ti.db real) e a autenticação passa a aceitar o
/// esquema de teste de <see cref="TestAuthHandler"/> como padrão, no lugar do cookie do
/// Identity. Todo o resto — autorização, filtros, model binding, antiforgery — roda como em
/// produção, que é justamente o que estes testes precisam exercitar.
/// </summary>
public sealed class ElcopWebApplicationFactory : WebApplicationFactory<Program>
{
    private DbConnection? _conexaoSqlite;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Elcop:Admin:Senha"] = "SenhaDeTeste!2026"
            }));

        builder.ConfigureServices(services =>
        {
            RemoverRegistro<DbContextOptions<AppDbContext>>(services);
            RemoverRegistro<DbConnection>(services);

            _conexaoSqlite = new SqliteConnection("DataSource=:memory:");
            _conexaoSqlite.Open();

            services.AddSingleton<DbConnection>(_conexaoSqlite);
            services.AddDbContext<AppDbContext>((sp, options) =>
                options.UseSqlite(sp.GetRequiredService<DbConnection>()));

            // Substitui o esquema padrão (cookie do Identity) pelo esquema de teste em
            // todas as etapas — autenticar, desafiar (401) e negar (403) — para que
            // [Authorize]/FallbackPolicy usem TestAuthHandler de ponta a ponta.
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = TestAuthHandler.Esquema;
                options.DefaultAuthenticateScheme = TestAuthHandler.Esquema;
                options.DefaultChallengeScheme = TestAuthHandler.Esquema;
                options.DefaultForbidScheme = TestAuthHandler.Esquema;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.Esquema, _ => { });
        });
    }

    private static void RemoverRegistro<T>(IServiceCollection services)
    {
        var descritor = services.SingleOrDefault(d => d.ServiceType == typeof(T));
        if (descritor is not null) services.Remove(descritor);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _conexaoSqlite?.Dispose();
    }
}
