using Elcop.TI.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Elcop.TI.Web.Controllers;

/// <summary>Tela inicial com os indicadores consolidados de inventário e demandas.</summary>
public class PainelController : Controller
{
    private readonly IPainelService _painel;
    private readonly IMovimentacaoService _movimentacoes;

    public PainelController(IPainelService painel, IMovimentacaoService movimentacoes)
    {
        _painel = painel;
        _movimentacoes = movimentacoes;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        // Reclassifica devoluções vencidas antes de contar os indicadores.
        await _movimentacoes.AtualizarAtrasosAsync(ct);

        return View(await _painel.ObterAsync(ct));
    }
}
