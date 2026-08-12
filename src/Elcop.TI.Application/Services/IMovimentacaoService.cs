using Elcop.TI.Application.Common;
using Elcop.TI.Application.Models;
using Elcop.TI.Domain.Entities;

namespace Elcop.TI.Application.Services;

public interface IMovimentacaoService
{
    Task<ResultadoPaginado<Movimentacao>> ListarAsync(MovimentacaoFiltro filtro, CancellationToken ct = default);

    Task<Movimentacao?> ObterAsync(int id, CancellationToken ct = default);

    /// <summary>Registra a retirada do ativo pelo colaborador e passa o ativo para "em uso".</summary>
    Task<int> RegistrarEntregaAsync(EntregaAtivoModel model, CancellationToken ct = default);

    /// <summary>Fecha o termo em aberto, devolvendo o ativo ao estoque com a condição informada.</summary>
    Task RegistrarDevolucaoAsync(DevolucaoAtivoModel model, CancellationToken ct = default);

    /// <summary>Devolve e reentrega o ativo a outro colaborador em uma única operação.</summary>
    Task<int> TransferirAsync(int movimentacaoId, int novoColaboradorId, string? observacoes, CancellationToken ct = default);

    Task CancelarAsync(int id, string motivo, CancellationToken ct = default);

    /// <summary>Termo em aberto de um ativo, ou null se ele estiver no estoque.</summary>
    Task<Movimentacao?> ObterEmAbertoDoAtivoAsync(int ativoId, CancellationToken ct = default);

    Task<IReadOnlyList<Movimentacao>> ListarEmAbertoAsync(CancellationToken ct = default);

    /// <summary>Reclassifica como "Atrasado" os termos cuja previsão de devolução expirou.</summary>
    Task<int> AtualizarAtrasosAsync(CancellationToken ct = default);

    /// <summary>Modelo pré-preenchido para a tela de devolução.</summary>
    Task<DevolucaoAtivoModel?> PrepararDevolucaoAsync(int movimentacaoId, CancellationToken ct = default);
}
