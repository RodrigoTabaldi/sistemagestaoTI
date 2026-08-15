using Elcop.TI.Application.Common;
using Elcop.TI.Application.Models;
using Elcop.TI.Application.Services;
using Elcop.TI.Domain.Entities;
using Elcop.TI.Web.Infra;
using Elcop.TI.Web.Models;
using Elcop.TI.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Elcop.TI.Web.Controllers;

/// <summary>Cadastro das pessoas que recebem ativos e abrem demandas.</summary>
public class ColaboradoresController : Controller
{
    private const string PastaFotos = "colaboradores";

    private readonly IColaboradorService _colaboradores;
    private readonly ISelecaoService _selecao;
    private readonly IArmazenamentoArquivos _arquivos;
    private readonly IBackgroundTaskQueue _filaBackground;

    public ColaboradoresController(
        IColaboradorService colaboradores, ISelecaoService selecao, IArmazenamentoArquivos arquivos,
        IBackgroundTaskQueue filaBackground)
    {
        _colaboradores = colaboradores;
        _selecao = selecao;
        _arquivos = arquivos;
        _filaBackground = filaBackground;
    }

    public async Task<IActionResult> Index(ColaboradorFiltro filtro, CancellationToken ct)
    {
        var pagina = await _colaboradores.ListarAsync(filtro, ct);

        // Contagem de ativos apenas dos colaboradores exibidos na página corrente.
        var contagem = await _colaboradores.ContarAtivosEmPosseAsync(
            pagina.Itens.Select(c => c.Id), ct);

        return View(new ListagemColaboradoresViewModel
        {
            Pagina = pagina,
            Filtro = filtro,
            Listas = await _selecao.MontarAsync(ct: ct),
            AtivosPorColaborador = contagem
        });
    }

    public async Task<IActionResult> Detalhes(int id, CancellationToken ct)
    {
        var colaborador = await _colaboradores.ObterCompletoAsync(id, ct);
        if (colaborador is null) return NotFound();

        return View(new DetalhesColaboradorViewModel
        {
            Colaborador = colaborador,
            AtivosEmPosse = await _colaboradores.ObterAtivosEmPosseAsync(id, ct),
            Historico = colaborador.Movimentacoes.OrderByDescending(m => m.DataRetirada).ToList(),
            Demandas = colaborador.DemandasSolicitadas.OrderByDescending(d => d.DataAbertura).ToList()
        });
    }

    [Authorize(Policy = Politicas.Operar)]
    public async Task<IActionResult> Criar(CancellationToken ct) =>
        View("Formulario", new ColaboradorFormViewModel
        {
            Colaborador = new Colaborador { DataAdmissao = DateTime.Today },
            Listas = await _selecao.MontarAsync(ct: ct)
        });

    [HttpPost]
    [Authorize(Policy = Politicas.Operar)]
    [EnableRateLimiting("upload")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(Colaborador colaborador, IFormFile? foto, CancellationToken ct)
    {
        this.ValidarFoto(foto);

        if (!ModelState.IsValid)
            return View("Formulario", new ColaboradorFormViewModel
            {
                Colaborador = colaborador,
                Listas = await _selecao.MontarAsync(ct: ct)
            });

        try
        {
            colaborador.FotoUrl =
                await UploadDeImagem.EnviarFotoAsync(_arquivos, foto, PastaFotos, ct) ?? colaborador.FotoUrl;
        }
        catch
        {
            ModelState.AddModelError(UploadDeImagem.Campo, "Não foi possível enviar a foto. Tente novamente.");
            return View("Formulario", new ColaboradorFormViewModel
            {
                Colaborador = colaborador,
                Listas = await _selecao.MontarAsync(ct: ct)
            });
        }

        var id = await _colaboradores.CriarAsync(colaborador, ct);
        this.NotificarSucesso($"Colaborador {colaborador.NomeCompleto} cadastrado.");

        return RedirectToAction(nameof(Detalhes), new { id });
    }

    [Authorize(Policy = Politicas.Operar)]
    public async Task<IActionResult> Editar(int id, CancellationToken ct)
    {
        var colaborador = await _colaboradores.ObterAsync(id, ct);
        if (colaborador is null) return NotFound();

        return View("Formulario", new ColaboradorFormViewModel
        {
            Colaborador = colaborador,
            Listas = await _selecao.MontarAsync(ct: ct)
        });
    }

    [HttpPost]
    [Authorize(Policy = Politicas.Operar)]
    [EnableRateLimiting("upload")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(
        int id, Colaborador colaborador, IFormFile? foto, CancellationToken ct)
    {
        if (id != colaborador.Id) return BadRequest();

        this.ValidarFoto(foto);

        if (!ModelState.IsValid)
            return View("Formulario", new ColaboradorFormViewModel
            {
                Colaborador = colaborador,
                Listas = await _selecao.MontarAsync(ct: ct)
            });

        var fotoAnterior = colaborador.FotoUrl;
        string? novaFoto = null;

        try
        {
            novaFoto = await UploadDeImagem.EnviarFotoAsync(_arquivos, foto, PastaFotos, ct);
            if (novaFoto is not null) colaborador.FotoUrl = novaFoto;
        }
        catch
        {
            ModelState.AddModelError(UploadDeImagem.Campo, "Não foi possível enviar a foto. Tente novamente.");
            return View("Formulario", new ColaboradorFormViewModel
            {
                Colaborador = colaborador,
                Listas = await _selecao.MontarAsync(ct: ct)
            });
        }

        await _colaboradores.AtualizarAsync(colaborador, ct);

        // Remover foto anterior em background: não bloqueia a resposta
        if (!string.IsNullOrWhiteSpace(fotoAnterior) && !string.IsNullOrWhiteSpace(novaFoto) && fotoAnterior != novaFoto)
        {
            await _filaBackground.EnfileirarAsync(async ct2 =>
                await UploadDeImagem.RemoverAnteriorAsync(_arquivos, fotoAnterior, novaFoto, ct2), ct);
        }

        this.NotificarSucesso("Cadastro atualizado.");

        return RedirectToAction(nameof(Detalhes), new { id });
    }

    [HttpPost]
    [Authorize(Policy = Politicas.Operar)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Excluir(int id, CancellationToken ct)
    {
        await _colaboradores.ExcluirAsync(id, ct);
        this.NotificarSucesso("Colaborador removido.");

        return RedirectToAction(nameof(Index));
    }

    [AcceptVerbs("GET", "POST")]
    public async Task<IActionResult> ValidarMatricula(string matricula, int id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(matricula)) return Json(true);

        var emUso = await _colaboradores.MatriculaEmUsoAsync(matricula, id == 0 ? null : id, ct);
        return Json(emUso ? $"A matrícula {matricula} já está cadastrada." : (object)true);
    }

    [AcceptVerbs("GET", "POST")]
    public async Task<IActionResult> ValidarEmail(string email, int id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email)) return Json(true);

        var emUso = await _colaboradores.EmailEmUsoAsync(email, id == 0 ? null : id, ct);
        return Json(emUso ? "Este e-mail já está cadastrado para outro colaborador." : (object)true);
    }
}
