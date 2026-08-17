using Elcop.TI.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Elcop.TI.Web.ViewComponents;

/// <summary>Contadores exibidos ao lado dos itens do menu lateral.</summary>
public record ContadoresMenu(int DemandasAbertas, int DevolucoesAtrasadas, int MovimentacoesEmAberto);

/// <summary>
/// Renderiza o menu lateral. Isolado em um view component para que os
/// contadores sejam consultados uma única vez por requisição.
/// </summary>
public class MenuLateralViewComponent : ViewComponent
{
    private readonly IDemandaService _demandas;
    private readonly IMovimentacaoService _movimentacoes;

    public MenuLateralViewComponent(IDemandaService demandas, IMovimentacaoService movimentacoes)
    {
        _demandas = demandas;
        _movimentacoes = movimentacoes;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (!(User.Identity?.IsAuthenticated ?? false))
            return View(new ContadoresMenu(0, 0, 0));

        var contadores = await _demandas.ObterContadoresAsync();
        var emAberto = await _movimentacoes.ListarEmAbertoAsync();

        return View(new ContadoresMenu(
            contadores.Abertas,
            emAberto.Count(m => m.EstaAtrasada),
            emAberto.Count));
    }
}
