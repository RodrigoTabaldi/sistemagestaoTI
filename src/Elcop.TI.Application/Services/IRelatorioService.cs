using Elcop.TI.Application.Models;

namespace Elcop.TI.Application.Services;

/// <summary>Exportações tabulares (CSV com BOM, separador ';' — abre direto no Excel pt-BR).</summary>
public interface IRelatorioService
{
    Task<byte[]> ExportarAtivosAsync(AtivoFiltro filtro, CancellationToken ct = default);

    Task<byte[]> ExportarColaboradoresAsync(ColaboradorFiltro filtro, CancellationToken ct = default);

    Task<byte[]> ExportarMovimentacoesAsync(MovimentacaoFiltro filtro, CancellationToken ct = default);

    Task<byte[]> ExportarDemandasAsync(DemandaFiltro filtro, CancellationToken ct = default);
}
