using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Elcop.TI.Infrastructure.Persistence.DesignTime;

/// <summary>
/// Usada pelo dotnet ef para gerar as migrations do contexto padrao com SQLite.
/// O banco real nao precisa existir para gerar migrations.
/// </summary>
public sealed class SqliteDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=elcop-ti-migrations.db", sqlite =>
                sqlite.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
            .Options;

        return new AppDbContext(options);
    }
}
