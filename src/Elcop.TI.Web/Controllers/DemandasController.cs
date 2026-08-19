using System.Security.Claims;
using Elcop.TI.Application.Models;
using Elcop.TI.Application.Services;
using Elcop.TI.Domain.Common;
using Elcop.TI.Domain.Entities;
using Elcop.TI.Domain.Enums;
using Elcop.TI.Web.Infra;
using Elcop.TI.Web.Models;
using Elcop.TI.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Elcop.TI.Web.Controllers;

/// <summary>Registro e acompanhamento das demandas de TI (lista e quadro kanban).</summary>
public class DemandasController : Controller
{
    private readonly IDemandaService _demandas;
    private readonly ISelecaoService _selecao;
    private readonly IColaboradorService _colaboradores;

    public DemandasController(
        IDemandaService demandas, ISelecaoService selecao, IColaboradorService colaboradores)
    {
        _demandas = demandas;
        _selecao = selecao;
        _colaboradores = colaboradores;
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

    /// <summary>
    /// Abertura de demanda, liberada a qualquer perfil: o usuário comum registra o chamado
    /// e a TI conduz o atendimento. Basta o título — todo o resto tem padrão.
    /// </summary>
    [Authorize(Policy = Politicas.AbrirDemanda)]
    public async Task<IActionResult> Criar(int? ativoId, int? solicitanteId, CancellationToken ct)
    {
        var podeOperar = User.PodeOperar();
        var solicitante = podeOperar ? null : await ObterColaboradorDoUsuarioAsync(ct);

        var demanda = new Demanda
        {
            DataAbertura = DateTime.Now,
            Prioridade = PrioridadeDemanda.Media,

            // Quem atende já entra como responsável; quem apenas solicita entra como
            // solicitante da própria demanda e só enxerga os equipamentos em posse dele.
            SolicitanteId = podeOperar ? solicitanteId : solicitante?.Id,
            Responsavel = podeOperar ? User.FindFirst(ClaimTypes.GivenName)?.Value : null,
            AtivoId = podeOperar
                ? ativoId
                : await FiltrarAtivoDoSolicitanteAsync(ativoId, solicitante?.Id, ct)
        };

        return View("Formulario", await MontarFormularioAsync(demanda, ct));
    }

    [HttpPost]
    [Authorize(Policy = Politicas.AbrirDemanda)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(Demanda demanda, CancellationToken ct)
    {
        IgnorarCamposDoServidor();

        if (!User.PodeOperar())
            await AplicarLimitesDoSolicitanteAsync(demanda, ct);

        if (!ModelState.IsValid)
            return View("Formulario", await MontarFormularioAsync(demanda, ct));

        var id = await _demandas.CriarAsync(demanda, ct);
        this.NotificarSucesso($"Demanda {demanda.Codigo} registrada.");

        return RedirectToAction(nameof(Detalhes), new { id });
    }

    [Authorize(Policy = Politicas.Operar)]
    public async Task<IActionResult> Editar(int id, CancellationToken ct)
    {
        var demanda = await _demandas.ObterAsync(id, ct);
        if (demanda is null) return NotFound();

        return View("Formulario", await MontarFormularioAsync(demanda, ct));
    }

    [HttpPost]
    [Authorize(Policy = Politicas.Operar)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, Demanda demanda, CancellationToken ct)
    {
        if (id != demanda.Id) return BadRequest();

        IgnorarCamposDoServidor();

        if (!ModelState.IsValid)
            return View("Formulario", await MontarFormularioAsync(demanda, ct));

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

    // ------------------------------------------------------------------ Formulário

    /// <summary>
    /// Descarta a validação dos campos que quem preenche não define — o serviço é que os
    /// gera (código) ou os preserva do registro existente (trilha de criação). Sem isto o
    /// <c>Codigo</c>, que é <c>string</c> não anulável, ganha um [Required] implícito do
    /// ASP.NET Core e chega vazio na abertura: a demanda é recusada e, por ser campo sem
    /// exibição no formulário, o usuário só vê a tela recarregar.
    /// </summary>
    private void IgnorarCamposDoServidor()
    {
        foreach (var campo in new[]
                 {
                     nameof(Demanda.Codigo), nameof(Demanda.CriadoEm), nameof(Demanda.CriadoPor),
                     nameof(Demanda.AtualizadoEm), nameof(Demanda.AtualizadoPor), nameof(Demanda.Ordem)
                 })
        {
            ModelState.Remove($"{nameof(DemandaFormViewModel.Demanda)}.{campo}");
        }
    }

    /// <summary>
    /// Monta o formulário conforme quem está preenchendo: a TI recebe as listas completas;
    /// o solicitante recebe só os equipamentos em posse dele — as demais listas nem são
    /// consultadas, já que ele não escolhe responsável, departamento nem outro solicitante.
    /// </summary>
    private async Task<DemandaFormViewModel> MontarFormularioAsync(Demanda demanda, CancellationToken ct)
    {
        if (User.PodeOperar())
            return new DemandaFormViewModel
            {
                Demanda = demanda,
                Listas = await _selecao.MontarAsync(
                    colaboradores: true, ativos: true, responsaveis: true, ct: ct)
            };

        return new DemandaFormViewModel
        {
            Demanda = demanda,
            Listas = new ListasDeSelecao
            {
                Ativos = await ListarAtivosDoSolicitanteAsync(demanda.SolicitanteId, ct)
            }
        };
    }

    /// <summary>
    /// Recorta a demanda ao que o solicitante decide — título, descrição, categoria,
    /// prioridade e o equipamento dele. A condução do atendimento (status, prazo,
    /// responsável, progresso, solução) volta ao padrão. Descartar os valores aqui, e não
    /// apenas escondê-los do formulário, é o que impede um POST forjado de defini-los.
    /// </summary>
    private async Task AplicarLimitesDoSolicitanteAsync(Demanda demanda, CancellationToken ct)
    {
        var solicitante = await ObterColaboradorDoUsuarioAsync(ct);

        demanda.SolicitanteId = solicitante?.Id;
        demanda.AtivoId = await FiltrarAtivoDoSolicitanteAsync(demanda.AtivoId, solicitante?.Id, ct);
        demanda.DepartamentoId = null;              // o serviço herda o do solicitante
        demanda.Status = StatusDemanda.Aberta;
        demanda.Responsavel = null;                 // a TI atribui na triagem
        demanda.PrazoLimite = null;                 // o SLA vem da prioridade
        demanda.DataAbertura = DateTime.Now;
        demanda.DataInicio = null;
        demanda.DataConclusao = null;
        demanda.PercentualConclusao = 0;
        demanda.TempoGastoMinutos = 0;
        demanda.Solucao = null;
        demanda.Tags = null;

        // Nenhum destes campos existe no formulário do solicitante: se vierem mesmo assim,
        // seus erros de validação não podem barrar a demanda que já foi corrigida acima.
        foreach (var campo in new[]
                 {
                     nameof(Demanda.SolicitanteId), nameof(Demanda.AtivoId), nameof(Demanda.DepartamentoId),
                     nameof(Demanda.Status), nameof(Demanda.Responsavel), nameof(Demanda.PrazoLimite),
                     nameof(Demanda.PercentualConclusao), nameof(Demanda.Solucao), nameof(Demanda.Tags)
                 })
        {
            ModelState.Remove($"{nameof(DemandaFormViewModel.Demanda)}.{campo}");
        }
    }

    /// <summary>
    /// Colaborador correspondente ao usuário logado (mesmo e-mail corporativo). É o que
    /// permite registrar quem pediu sem obrigar o solicitante a se procurar numa lista.
    /// </summary>
    private Task<Colaborador?> ObterColaboradorDoUsuarioAsync(CancellationToken ct)
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name;

        return string.IsNullOrWhiteSpace(email)
            ? Task.FromResult<Colaborador?>(null)
            : _colaboradores.ObterPorEmailAsync(email, ct);
    }

    /// <summary>Vínculo com ativo restrito aos equipamentos em posse de quem abre a demanda.</summary>
    private async Task<int?> FiltrarAtivoDoSolicitanteAsync(
        int? ativoId, int? colaboradorId, CancellationToken ct)
    {
        if (ativoId is null || colaboradorId is null) return null;

        var ativos = await _colaboradores.ObterAtivosEmPosseAsync(colaboradorId.Value, ct);
        return ativos.Any(a => a.Id == ativoId) ? ativoId : null;
    }

    private async Task<IEnumerable<SelectListItem>> ListarAtivosDoSolicitanteAsync(
        int? colaboradorId, CancellationToken ct)
    {
        if (colaboradorId is null) return Enumerable.Empty<SelectListItem>();

        var ativos = await _colaboradores.ObterAtivosEmPosseAsync(colaboradorId.Value, ct);

        return ativos
            .Select(a => new SelectListItem(
                $"{a.Patrimonio} · {a.Tipo.ObterNome()} · {a.DescricaoCurta}", a.Id.ToString()))
            .ToList();
    }
}
