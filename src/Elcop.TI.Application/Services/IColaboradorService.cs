using Elcop.TI.Application.Common;
using Elcop.TI.Application.Models;
using Elcop.TI.Domain.Entities;

namespace Elcop.TI.Application.Services;

public interface IColaboradorService
{
    Task<ResultadoPaginado<Colaborador>> ListarAsync(ColaboradorFiltro filtro, CancellationToken ct = default);

    Task<Colaborador?> ObterAsync(int id, CancellationToken ct = default);

    Task<Colaborador?> ObterCompletoAsync(int id, CancellationToken ct = default);

    Task<int> CriarAsync(Colaborador colaborador, CancellationToken ct = default);

    Task AtualizarAsync(Colaborador colaborador, CancellationToken ct = default);

    /// <summary>Exclusão lógica; bloqueada enquanto houver ativos em posse do colaborador.</summary>
    Task ExcluirAsync(int id, CancellationToken ct = default);

    Task<IReadOnlyList<Ativo>> ObterAtivosEmPosseAsync(int colaboradorId, CancellationToken ct = default);

    /// <summary>Quantidade de ativos em posse de cada colaborador informado (uma única consulta).</summary>
    Task<IReadOnlyDictionary<int, int>> ContarAtivosEmPosseAsync(
        IEnumerable<int> colaboradorIds, CancellationToken ct = default);

    Task<IReadOnlyList<Colaborador>> ListarParaSelecaoAsync(CancellationToken ct = default);

    /// <summary>
    /// Colaborador cujo e-mail corporativo corresponde ao informado. Usado para ligar o
    /// usuário logado à pessoa do cadastro quando ele abre uma demanda.
    /// </summary>
    Task<Colaborador?> ObterPorEmailAsync(string email, CancellationToken ct = default);

    Task<bool> MatriculaEmUsoAsync(string matricula, int? ignorarId = null, CancellationToken ct = default);

    Task<bool> EmailEmUsoAsync(string email, int? ignorarId = null, CancellationToken ct = default);
}
