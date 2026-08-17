using Elcop.TI.Application.Models;
using Elcop.TI.Application.Services;
using Elcop.TI.Domain.Common;
using Elcop.TI.Domain.Enums;
using Elcop.TI.Web.Infra;
using Elcop.TI.Web.Models;
using Elcop.TI.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elcop.TI.Web.Controllers;

/// <summary>
/// Retirada e devolução de ativos — os "slots" de entrada e saída do estoque de TI.
/// </summary>
public class MovimentacoesController : Controller
{
    private readonly IMovimentacaoService _movimentacoes;
    private readonly IAtivoService _ativos;
    private readonly IColaboradorService _colaboradores;
    private readonly ISelecaoService _selecao;

    public MovimentacoesController(
        IMovimentacaoService movimentacoes,
        IAtivoService ativos,
        IColaboradorService colaboradores,
        ISelecaoService selecao)
    {
        _movimentacoes = movimentacoes;
        _ativos = ativos;
        _colaboradores = colaboradores;
        _selecao = selecao;
    }

    public async Task<IActionResult> Index(MovimentacaoFiltro filtro, CancellationToken ct)
    {
        await _movimentacoes.AtualizarAtrasosAsync(ct);

        var emAberto = await _movimentacoes.ListarEmAbertoAsync(ct);

        return View(new ListagemMovimentacoesViewModel
        {
            Pagina = await _movimentacoes.ListarAsync(filtro, ct),
            Filtro = filtro,
            Listas = await _selecao.MontarAsync(colaboradores: true, ct: ct),
            TotalEmAberto = emAberto.Count,
            TotalAtrasadas = emAberto.Count(m => m.EstaAtrasada)
        });
    }

    public async Task<IActionResult> Detalhes(int id, CancellationToken ct)
    {
        var movimentacao = await _movimentacoes.ObterAsync(id, ct);
        if (movimentacao is null) return NotFound();

        return View(movimentacao);
    }

    /// <summary>Termo de responsabilidade pronto para impressão/assinatura.</summary>
    public async Task<IActionResult> Termo(int id, CancellationToken ct)
    {
        var movimentacao = await _movimentacoes.ObterAsync(id, ct);
        if (movimentacao is null) return NotFound();

        return View(movimentacao);
    }

    // ------------------------------------------------------------------ Entrega

    [Authorize(Policy = Politicas.Operar)]
    public async Task<IActionResult> Entregar(int? ativoId, int? colaboradorId, CancellationToken ct)
    {
        var modelo = await MontarEntregaAsync(new EntregaAtivoModel
        {
            AtivoId = ativoId ?? 0,
            ColaboradorId = colaboradorId ?? 0,
            DataRetirada = DateTime.Now,
            ResponsavelEntrega = User.FindFirst(System.Security.Claims.ClaimTypes.GivenName)?.Value,
            LocalEntrega = "Almoxarifado de TI"
        }, ct);

        return View(modelo);
    }

    [HttpPost]
    [Authorize(Policy = Politicas.Operar)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Entregar(EntregaAtivoModel entrega, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(await MontarEntregaAsync(entrega, ct));

        var id = await _movimentacoes.RegistrarEntregaAsync(entrega, ct);
        this.NotificarSucesso("Entrega registrada. O termo de responsabilidade já pode ser impresso.");

        return RedirectToAction(nameof(Detalhes), new { id });
    }

    // ------------------------------------------------------------------ Devolução

    [Authorize(Policy = Politicas.Operar)]
    public async Task<IActionResult> Devolver(int id, CancellationToken ct)
    {
        var movimentacao = await _movimentacoes.ObterAsync(id, ct);
        if (movimentacao is null) return NotFound();

        if (!movimentacao.EstaEmAberto)
        {
            this.NotificarAviso($"O termo {movimentacao.Protocolo} já foi encerrado.");
            return RedirectToAction(nameof(Detalhes), new { id });
        }

        var devolucao = await _movimentacoes.PrepararDevolucaoAsync(id, ct);
        if (devolucao is null) return NotFound();

        return View(new DevolucaoViewModel { Devolucao = devolucao, Movimentacao = movimentacao });
    }

    [HttpPost]
    [Authorize(Policy = Politicas.Operar)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Devolver(DevolucaoAtivoModel devolucao, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            var movimentacao = await _movimentacoes.ObterAsync(devolucao.MovimentacaoId, ct);
            if (movimentacao is null) return NotFound();

            return View(new DevolucaoViewModel { Devolucao = devolucao, Movimentacao = movimentacao });
        }

        await _movimentacoes.RegistrarDevolucaoAsync(devolucao, ct);
        this.NotificarSucesso("Devolução registrada. O ativo voltou ao estoque.");

        return RedirectToAction(nameof(Detalhes), new { id = devolucao.MovimentacaoId });
    }

    // ------------------------------------------------------------------ Transferência

    [Authorize(Policy = Politicas.Operar)]
    public async Task<IActionResult> Transferir(int id, CancellationToken ct)
    {
        var movimentacao = await _movimentacoes.ObterAsync(id, ct);
        if (movimentacao is null) return NotFound();

        return View(new TransferenciaViewModel
        {
            MovimentacaoId = id,
            Movimentacao = movimentacao,
            Listas = await _selecao.MontarAsync(colaboradores: true, ct: ct)
        });
    }

    [HttpPost]
    [Authorize(Policy = Politicas.Operar)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Transferir(TransferenciaViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(new TransferenciaViewModel
            {
                MovimentacaoId = model.MovimentacaoId,
                NovoColaboradorId = model.NovoColaboradorId,
                Observacoes = model.Observacoes,
                Movimentacao = await _movimentacoes.ObterAsync(model.MovimentacaoId, ct),
                Listas = await _selecao.MontarAsync(colaboradores: true, ct: ct)
            });
        }

        var novoId = await _movimentacoes.TransferirAsync(
            model.MovimentacaoId, model.NovoColaboradorId, model.Observacoes, ct);

        this.NotificarSucesso("Transferência concluída: novo termo gerado para o colaborador de destino.");
        return RedirectToAction(nameof(Detalhes), new { id = novoId });
    }

    [HttpPost]
    [Authorize(Policy = Politicas.Operar)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancelar(int id, string motivo, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(motivo))
        {
            this.NotificarErro("Informe o motivo do cancelamento.");
            return RedirectToAction(nameof(Detalhes), new { id });
        }

        await _movimentacoes.CancelarAsync(id, motivo, ct);
        this.NotificarSucesso("Movimentação cancelada.");

        return RedirectToAction(nameof(Detalhes), new { id });
    }

    /// <summary>
    /// Monta a tela de entrega com os catálogos que alimentam a pré-visualização
    /// dinâmica do ativo e do colaborador selecionados.
    /// </summary>
    private async Task<EntregaViewModel> MontarEntregaAsync(EntregaAtivoModel entrega, CancellationToken ct)
    {
        var disponiveis = await _ativos.ListarDisponiveisAsync(
            entrega.AtivoId == 0 ? null : entrega.AtivoId, ct);

        var pessoas = await _colaboradores.ListarParaSelecaoAsync(ct);
        var contagens = await _colaboradores.ContarAtivosEmPosseAsync(pessoas.Select(p => p.Id), ct);

        return new EntregaViewModel
        {
            Entrega = entrega,
            Listas = await _selecao.MontarAsync(
                colaboradores: true, ativos: true,
                ativoSelecionado: entrega.AtivoId == 0 ? null : entrega.AtivoId, ct: ct),

            CatalogoAtivos = disponiveis.Select(a => new ResumoAtivoJson(
                a.Id, a.Patrimonio, a.Tipo.ObterNome(), a.DescricaoCurta, a.NumeroSerie,
                a.Imei, a.NumeroLinha, a.Condicao.ObterNome(), a.Acessorios,
                ViewHelpers.IconeTipoAtivo(a.Tipo))).ToList(),

            CatalogoColaboradores = pessoas.Select(c => new ResumoColaboradorJson(
                c.Id, c.NomeCompleto, c.Matricula, c.Email, c.Cargo,
                c.Departamento?.Nome, c.Iniciais, contagens.GetValueOrDefault(c.Id))).ToList()
        };
    }
}
