using Elcop.TI.Application.Common;
using Elcop.TI.Application.Models;
using Elcop.TI.Domain.Entities;
using Elcop.TI.Domain.Enums;

namespace Elcop.TI.Application.Services;

public interface IAtivoService
{
    Task<ResultadoPaginado<Ativo>> ListarAsync(AtivoFiltro filtro, CancellationToken ct = default);

    Task<Ativo?> ObterAsync(int id, CancellationToken ct = default);

    /// <summary>Ativo com movimentações e relacionamentos carregados, para a tela de detalhes.</summary>
    Task<Ativo?> ObterCompletoAsync(int id, CancellationToken ct = default);

    Task<int> CriarAsync(Ativo ativo, CancellationToken ct = default);

    Task AtualizarAsync(Ativo ativo, CancellationToken ct = default);

    /// <summary>Exclusão lógica; bloqueada enquanto o ativo estiver em posse de alguém.</summary>
    Task ExcluirAsync(int id, CancellationToken ct = default);

    Task AlterarStatusAsync(int id, StatusAtivo status, string? motivo, CancellationToken ct = default);

    Task<IReadOnlyList<Movimentacao>> ObterHistoricoAsync(int ativoId, CancellationToken ct = default);

    /// <summary>Próximo número de patrimônio sugerido no padrão TI-000000.</summary>
    Task<string> SugerirPatrimonioAsync(CancellationToken ct = default);

    Task<bool> PatrimonioEmUsoAsync(string patrimonio, int? ignorarId = null, CancellationToken ct = default);

    Task<bool> NumeroSerieEmUsoAsync(string numeroSerie, int? ignorarId = null, CancellationToken ct = default);

    /// <summary>Ativos livres para entrega, opcionalmente incluindo um já selecionado.</summary>
    Task<IReadOnlyList<Ativo>> ListarDisponiveisAsync(int? incluirId = null, CancellationToken ct = default);

    Task<IReadOnlyList<ResumoPorTipo>> ResumirPorTipoAsync(CancellationToken ct = default);
}
