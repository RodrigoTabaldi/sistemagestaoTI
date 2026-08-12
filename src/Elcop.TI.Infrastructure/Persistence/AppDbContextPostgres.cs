using Elcop.TI.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Elcop.TI.Infrastructure.Persistence;

/// <summary>
/// Contexto usado quando o provedor configurado é o PostgreSQL (Cloud SQL / Firebase Data Connect).
///
/// O modelo, as configurações Fluent API e as regras de auditoria são exatamente os do
/// <see cref="AppDbContext"/> — a única razão de existir um tipo separado é que as migrations
/// do EF Core são específicas do provedor e ficam indexadas pelo tipo do contexto. Assim o
/// conjunto SQLite (<c>Persistence/Migrations</c>) e o conjunto Postgres
/// (<c>Persistence/Migrations/Postgres</c>) convivem sem conflito.
/// </summary>
public sealed class AppDbContextPostgres : AppDbContext
{
    public AppDbContextPostgres(
        DbContextOptions<AppDbContextPostgres> options, IUsuarioAtual? usuarioAtual = null)
        : base(options, usuarioAtual)
    {
    }
}
