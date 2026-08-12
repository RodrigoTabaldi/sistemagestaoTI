using Elcop.TI.Application.Common;
using Elcop.TI.Domain.Entities;
using Elcop.TI.Domain.Enums;

namespace Elcop.TI.Application.Services;

public interface IAuditoriaService
{
    /// <summary>Enfileira um registro de auditoria (persistido junto do SaveChanges do serviço chamador).</summary>
    Task RegistrarAsync(
        TipoAcaoAuditoria acao,
        string entidade,
        int? entidadeId,
        string descricao,
        CancellationToken ct = default);

    /// <summary>
    /// Registra e persiste imediatamente. Usado por fluxos que não têm um
    /// SaveChanges próprio na sequência (login, logout, exportações).
    /// </summary>
    Task RegistrarESalvarAsync(
        TipoAcaoAuditoria acao,
        string entidade,
        int? entidadeId,
        string descricao,
        CancellationToken ct = default);

    Task<ResultadoPaginado<RegistroAuditoria>> ListarAsync(
        string? busca,
        TipoAcaoAuditoria? acao,
        DateTime? de,
        DateTime? ate,
        int pagina,
        int tamanhoPagina,
        CancellationToken ct = default);
}
