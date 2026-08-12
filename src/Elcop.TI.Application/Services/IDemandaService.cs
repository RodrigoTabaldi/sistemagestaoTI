using Elcop.TI.Application.Common;
using Elcop.TI.Application.Models;
using Elcop.TI.Domain.Entities;
using Elcop.TI.Domain.Enums;

namespace Elcop.TI.Application.Services;

public interface IDemandaService
{
    Task<ResultadoPaginado<Demanda>> ListarAsync(DemandaFiltro filtro, CancellationToken ct = default);

    /// <summary>Quadro kanban agrupado por status, respeitando o filtro corrente.</summary>
    Task<QuadroKanban> ObterQuadroAsync(DemandaFiltro filtro, CancellationToken ct = default);

    Task<Demanda?> ObterAsync(int id, CancellationToken ct = default);

    Task<Demanda?> ObterCompletaAsync(int id, CancellationToken ct = default);

    Task<int> CriarAsync(Demanda demanda, CancellationToken ct = default);

    Task AtualizarAsync(Demanda demanda, CancellationToken ct = default);

    Task ExcluirAsync(int id, CancellationToken ct = default);

    /// <summary>Adiciona um andamento e, se informado, aplica a transição de status.</summary>
    Task AdicionarAndamentoAsync(NovoAndamentoModel model, CancellationToken ct = default);

    /// <summary>Move a demanda de coluna no kanban (arrastar e soltar).</summary>
    Task MoverAsync(int id, StatusDemanda novoStatus, int? novaOrdem, CancellationToken ct = default);

    Task<IReadOnlyList<string>> ListarResponsaveisAsync(CancellationToken ct = default);

    /// <summary>Contadores do cabeçalho da listagem, calculados sobre a base inteira.</summary>
    Task<ContadoresDemanda> ObterContadoresAsync(CancellationToken ct = default);
}
