using Elcop.TI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Elcop.TI.Application.Common;

/// <summary>
/// Abstração do contexto de dados consumida pelos serviços de aplicação.
/// Mantém a camada Application livre de detalhes do provedor (SQLite/SQL Server),
/// que ficam confinados à Infrastructure.
/// </summary>
public interface IAppDbContext
{
    DbSet<Ativo> Ativos { get; }
    DbSet<Colaborador> Colaboradores { get; }
    DbSet<Departamento> Departamentos { get; }
    DbSet<Localizacao> Localizacoes { get; }
    DbSet<Fornecedor> Fornecedores { get; }
    DbSet<Movimentacao> Movimentacoes { get; }
    DbSet<Demanda> Demandas { get; }
    DbSet<DemandaAndamento> DemandaAndamentos { get; }
    DbSet<RegistroAuditoria> RegistrosAuditoria { get; }

    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
