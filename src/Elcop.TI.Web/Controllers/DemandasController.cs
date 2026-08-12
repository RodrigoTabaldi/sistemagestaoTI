using Elcop.TI.Application.Models;
using Elcop.TI.Application.Services;
using Elcop.TI.Domain.Entities;
using Elcop.TI.Domain.Enums;
using Elcop.TI.Web.Infra;
using Elcop.TI.Web.Models;
using Elcop.TI.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elcop.TI.Web.Controllers;

/// <summary>Registro e acompanhamento das demandas de TI (lista e quadro kanban).</summary>
public class DemandasController : Controller
{
    private readonly IDemandaService _demandas;
    private readonly ISelecaoService _selecao;

    public DemandasController(IDemandaService demandas, ISelecaoService selecao)
    {
        _demandas = demandas;
        _selecao = selecao;
    }

    public async Task<IActionResult> Index(DemandaFiltro filtro, CancellationToken ct)
    {
        // Os contadores do topo refletem a base inteira, não o filtro aplicado.
        var contadores = await _demandas.ObterContadoresAsync(ct);

        return View(new ListagemDemandasViewModel
        {
            Pagina = await _demandas.ListarAsync(filtro, ct),
            Filtro = filtro,
            Listas = await _selecao.MontarAsync(colaboradores: true, ativos: true, responsaveis: true, ct: ct),
            TotalAbertas = contadores.Abertas,
            TotalAtrasadas = contadores.Atrasadas,
            TotalConcluidasMes = contadores.ConcluidasNoMes
        });
    }

    /// <summary>Quadro kanban com arrastar e soltar entre as colunas de status.</summary>
    public async Task<IActionResult> Quadro(DemandaFiltro filtro, CancellationToken ct) =>
        View(new QuadroDemandasViewModel
        {
            Quadro = await _demandas.ObterQuadroAsync(filtro, ct),
            Listas = await _selecao.MontarAsync(colaboradores: true, responsaveis: true, ct: ct)
        });

    public async Task<IActionResult> Detalhes(int id, CancellationToken ct)
    {
        var demanda = await _demandas.ObterCompletaAsync(id, ct);
        if (demanda is null) return NotFound();

        return View(new DetalhesDemandaViewModel
        {
            Demanda = demanda,
            NovoAndamento = new NovoAndamentoModel { DemandaId = id, NovoStatus = demanda.Status }
        });
    }

    [Authorize(Policy = Politicas.Operar)]
    public async Task<IActionResult> Criar(int? ativoId, int? solicitanteId, CancellationToken ct) =>
        View("Formulario", new DemandaFormViewModel
        {
            Demanda = new Demanda
            {
                DataAbertura = DateTime.Now,
                AtivoId = ativoId,
                SolicitanteId = solicitanteId,
                Prioridade = PrioridadeDemanda.Media,
                Responsavel = User.FindFirst(System.Security.Claims.ClaimTypes.GivenName)?.Value
            },
            Listas = await _selecao.MontarAsync(colaboradores: true, ativos: true, responsaveis: true, ct: ct)
        });

    [HttpPost]
    [Authorize(Policy = Politicas.Operar)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(Demanda demanda, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View("Formulario", new DemandaFormViewModel
            {
                Demanda = demanda,
                Listas = await _selecao.MontarAsync(colaboradores: true, ativos: true, responsaveis: true, ct: ct)
            });

        var id = await _demandas.CriarAsync(demanda, ct);
        this.NotificarSucesso($"Demanda {demanda.Codigo} registrada.");

        return RedirectToAction(nameof(Detalhes), new { id });
    }

    [Authorize(Policy = Politicas.Operar)]
    public async Task<IActionResult> Editar(int id, CancellationToken ct)
    {
        var demanda = await _demandas.ObterAsync(id, ct);
        if (demanda is null) return NotFound();

        return View("Formulario", new DemandaFormViewModel
        {
            Demanda = demanda,
            Listas = await _selecao.MontarAsync(colaboradores: true, ativos: true, responsaveis: true, ct: ct)
        });
    }

    [HttpPost]
    [Authorize(Policy = Politicas.Operar)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, Demanda demanda, CancellationToken ct)
    {
        if (id != demanda.Id) return BadRequest();

        if (!ModelState.IsValid)
            return View("Formulario", new DemandaFormViewModel
            {
                Demanda = demanda,
                Listas = await _selecao.MontarAsync(colaboradores: true, ativos: true, responsaveis: true, ct: ct)
            });

        await _demandas.AtualizarAsync(demanda, ct);
        this.NotificarSucesso("Demanda atualizada.");

        return RedirectToAction(nameof(Detalhes), new { id });
    }

    [HttpPost]
    [Authorize(Policy = Politicas.Operar)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Andamento(NovoAndamentoModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            this.NotificarErro("Descreva o andamento antes de registrar.");
            return RedirectToAction(nameof(Detalhes), new { id = model.DemandaId });
        }

        await _demandas.AdicionarAndamentoAsync(model, ct);
        this.NotificarSucesso("Andamento registrado na linha do tempo.");

        return RedirectToAction(nameof(Detalhes), new { id = model.DemandaId });
    }

    /// <summary>Endpoint do arrastar e soltar do quadro kanban.</summary>
    [HttpPost]
    [Authorize(Policy = Politicas.Operar)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Mover(
        int id, StatusDemanda status, int? ordem, CancellationToken ct)
    {
        await _demandas.MoverAsync(id, status, ordem, ct);
        return Json(new { sucesso = true });
    }

    [HttpPost]
    [Authorize(Policy = Politicas.Operar)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Excluir(int id, CancellationToken ct)
    {
        await _demandas.ExcluirAsync(id, ct);
        this.NotificarSucesso("Demanda excluída.");

        return RedirectToAction(nameof(Index));
    }
}
