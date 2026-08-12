using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Elcop.TI.Infrastructure.Persistence.DesignTime;

/// <summary>
/// Usada apenas pelo <c>dotnet ef</c> para gerar as migrations do PostgreSQL sem
/// precisar subir a aplicação. A connection string vem da variável de ambiente
/// <c>Elcop__ConnectionStringPostgres</c>; na ausência dela usa um valor local, já
/// que para gerar migrations o EF Core só precisa do provedor, não de um banco de verdade.
///
///   dotnet ef migrations add EstruturaInicial ^
///     --project src\Elcop.TI.Infrastructure ^
///     --startup-project src\Elcop.TI.Web ^
///     --context AppDbContextPostgres ^
///     --output-dir Persistence\Migrations\Postgres
/// </summary>
public sealed class PostgresDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContextPostgres>
{
    public AppDbContextPostgres CreateDbContext(string[] args)
    {
        var conexao = Environment.GetEnvironmentVariable("Elcop__ConnectionStringPostgres")
            ?? "Host=localhost;Port=5432;Database=elcopti;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<AppDbContextPostgres>()
            .UseNpgsql(conexao, npg =>
            {
                npg.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                npg.MigrationsHistoryTable("__EFMigrationsHistory");
            })
            .Options;

        return new AppDbContextPostgres(options);
    }
}
