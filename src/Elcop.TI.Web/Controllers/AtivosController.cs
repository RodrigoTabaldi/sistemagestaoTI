using Elcop.TI.Application.Common;
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

namespace Elcop.TI.Web.Controllers;

/// <summary>Inventário de ativos de TI: cadastro, consulta e ciclo de vida.</summary>
public class AtivosController : Controller
{
    private const string PastaFotos = "ativos";

    private readonly IAtivoService _ativos;
    private readonly ISelecaoService _selecao;
    private readonly IArmazenamentoArquivos _arquivos;

    public AtivosController(
        IAtivoService ativos, ISelecaoService selecao, IArmazenamentoArquivos arquivos)
    {
        _ativos = ativos;
        _selecao = selecao;
        _arquivos = arquivos;
    }

    public async Task<IActionResult> Index(AtivoFiltro filtro, CancellationToken ct)
    {
        var modelo = new ListagemAtivosViewModel
        {
            Pagina = await _ativos.ListarAsync(filtro, ct),
            Filtro = filtro,
            Listas = await _selecao.MontarAsync(colaboradores: true, ct: ct),
            Resumo = await _ativos.ResumirPorTipoAsync(ct)
        };

        return View(modelo);
    }

    public async Task<IActionResult> Detalhes(int id, CancellationToken ct)
    {
        var ativo = await _ativos.ObterCompletoAsync(id, ct);
        if (ativo is null) return NotFound();

        var posse = ativo.Movimentacoes.FirstOrDefault(m => m.EstaEmAberto);

        return View(new DetalhesAtivoViewModel
        {
            Ativo = ativo,
            PosseAtual = posse,
            Historico = ativo.Movimentacoes.OrderByDescending(m => m.DataRetirada).ToList(),
            Demandas = ativo.Demandas.OrderByDescending(d => d.DataAbertura).ToList()
        });
    }

    [Authorize(Policy = Politicas.Operar)]
    public async Task<IActionResult> Criar(CancellationToken ct)
    {
        var modelo = new AtivoFormViewModel
        {
            Ativo = new Ativo
            {
                Patrimonio = await _ativos.SugerirPatrimonioAsync(ct),
                DataAquisicao = DateTime.Today,
                Status = StatusAtivo.Disponivel,
                Condicao = CondicaoAtivo.Novo
            },
            Listas = await _selecao.MontarAsync(ct: ct)
        };

        return View("Formulario", modelo);
    }

    [HttpPost]
    [Authorize(Policy = Politicas.Operar)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(Ativo ativo, IFormFile? foto, CancellationToken ct)
    {
        this.ValidarFoto(foto);

        if (!ModelState.IsValid)
            return View("Formulario", new AtivoFormViewModel
            {
                Ativo = ativo,
                Listas = await _selecao.MontarAsync(ct: ct)
            });

        ativo.FotoUrl = await UploadDeImagem.EnviarFotoAsync(_arquivos, foto, PastaFotos, ct) ?? ativo.FotoUrl;

        var id = await _ativos.CriarAsync(ativo, ct);
        this.NotificarSucesso($"Ativo {ativo.Patrimonio} cadastrado com sucesso.");

        return RedirectToAction(nameof(Detalhes), new { id });
    }

    [Authorize(Policy = Politicas.Operar)]
    public async Task<IActionResult> Editar(int id, CancellationToken ct)
    {
        var ativo = await _ativos.ObterAsync(id, ct);
        if (ativo is null) return NotFound();

        return View("Formulario", new AtivoFormViewModel
        {
            Ativo = ativo,
            Listas = await _selecao.MontarAsync(ct: ct)
        });
    }

    [HttpPost]
    [Authorize(Policy = Politicas.Operar)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, Ativo ativo, IFormFile? foto, CancellationToken ct)
    {
        if (id != ativo.Id) return BadRequest();

        this.ValidarFoto(foto);

        if (!ModelState.IsValid)
            return View("Formulario", new AtivoFormViewModel
            {
                Ativo = ativo,
                Listas = await _selecao.MontarAsync(ct: ct)
            });

        var fotoAnterior = ativo.FotoUrl;
        var novaFoto = await UploadDeImagem.EnviarFotoAsync(_arquivos, foto, PastaFotos, ct);
        if (novaFoto is not null) ativo.FotoUrl = novaFoto;

        await _ativos.AtualizarAsync(ativo, ct);

        // Só depois de gravar: se a atualização falhar, a imagem antiga continua válida.
        await UploadDeImagem.RemoverAnteriorAsync(_arquivos, fotoAnterior, novaFoto, ct);

        this.NotificarSucesso($"Ativo {ativo.Patrimonio} atualizado.");

        return RedirectToAction(nameof(Detalhes), new { id });
    }

    [HttpPost]
    [Authorize(Policy = Politicas.Operar)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Excluir(int id, CancellationToken ct)
    {
        await _ativos.ExcluirAsync(id, ct);
        this.NotificarSucesso("Ativo removido do inventário.");

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Policy = Politicas.Operar)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AlterarStatus(
        int id, StatusAtivo status, string? motivo, CancellationToken ct)
    {
        await _ativos.AlterarStatusAsync(id, status, motivo, ct);
        this.NotificarSucesso($"Status alterado para \"{status.ObterNome()}\".");

        return RedirectToAction(nameof(Detalhes), new { id });
    }

    /// <summary>Ficha imprimível do ativo, com histórico de posse.</summary>
    public async Task<IActionResult> Ficha(int id, CancellationToken ct)
    {
        var ativo = await _ativos.ObterCompletoAsync(id, ct);
        if (ativo is null) return NotFound();

        return View(new DetalhesAtivoViewModel
        {
            Ativo = ativo,
            PosseAtual = ativo.Movimentacoes.FirstOrDefault(m => m.EstaEmAberto),
            Historico = ativo.Movimentacoes.OrderByDescending(m => m.DataRetirada).Take(20).ToList()
        });
    }

    /// <summary>Validação remota do patrimônio no formulário (jQuery Validate).</summary>
    [AcceptVerbs("GET", "POST")]
    public async Task<IActionResult> ValidarPatrimonio(string patrimonio, int id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(patrimonio)) return Json(true);

        var emUso = await _ativos.PatrimonioEmUsoAsync(patrimonio, id == 0 ? null : id, ct);
        return Json(emUso ? $"O patrimônio {patrimonio.ToUpperInvariant()} já está cadastrado." : (object)true);
    }

    [AcceptVerbs("GET", "POST")]
    public async Task<IActionResult> ValidarNumeroSerie(string numeroSerie, int id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(numeroSerie)) return Json(true);

        var emUso = await _ativos.NumeroSerieEmUsoAsync(numeroSerie, id == 0 ? null : id, ct);
        return Json(emUso ? $"O número de série {numeroSerie} já pertence a outro ativo." : (object)true);
    }
}
