using Elcop.TI.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Elcop.TI.Application.Tests;

/// <summary>
/// Banco Sqlite em memória, isolado por instância de teste (conexão exclusiva mantida
/// aberta enquanto o teste roda). Usa o mesmo <see cref="AppDbContext"/> da aplicação,
/// então os global query filters de exclusão lógica e o carimbo de auditoria valem
/// aqui exatamente como valem em produção.
/// </summary>
public sealed class TestDbContextFactory : IDisposable
{
    private readonly SqliteConnection _conexao;

    public TestDbContextFactory()
    {
        _conexao = new SqliteConnection("DataSource=:memory:");
        _conexao.Open();

        using var db = Criar();
        db.Database.EnsureCreated();
    }

    public AppDbContext Criar(FakeUsuarioAtual? usuario = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_conexao)
            .Options;

        return new AppDbContext(options, usuario ?? new FakeUsuarioAtual());
    }

    public void Dispose() => _conexao.Dispose();
}
