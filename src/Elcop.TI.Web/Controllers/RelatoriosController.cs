using Elcop.TI.Application.Models;
using Elcop.TI.Application.Services;
using Elcop.TI.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Elcop.TI.Web.Controllers;

/// <summary>Painel analítico e exportações em CSV.</summary>
public class RelatoriosController : Controller
{
    private const string TipoCsv = "text/csv";

    private readonly IRelatorioService _relatorios;
    private readonly IAtivoService _ativos;
    private readonly IPainelService _painel;
    private readonly IAuditoriaService _auditoria;

    public RelatoriosController(
        IRelatorioService relatorios,
        IAtivoService ativos,
        IPainelService painel,
        IAuditoriaService auditoria)
    {
        _relatorios = relatorios;
        _ativos = ativos;
        _painel = painel;
        _auditoria = auditoria;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Resumo"] = await _ativos.ResumirPorTipoAsync(ct);
        return View(await _painel.ObterAsync(ct));
    }

    public async Task<IActionResult> Ativos(AtivoFiltro filtro, CancellationToken ct) =>
        await ExportarAsync(
            () => _relatorios.ExportarAtivosAsync(filtro, ct), "ativos", "Inventário de ativos", ct);

    public async Task<IActionResult> Colaboradores(ColaboradorFiltro filtro, CancellationToken ct) =>
        await ExportarAsync(
            () => _relatorios.ExportarColaboradoresAsync(filtro, ct), "colaboradores", "Colaboradores", ct);

    public async Task<IActionResult> Movimentacoes(MovimentacaoFiltro filtro, CancellationToken ct) =>
        await ExportarAsync(
            () => _relatorios.ExportarMovimentacoesAsync(filtro, ct), "movimentacoes", "Movimentações", ct);

    public async Task<IActionResult> Demandas(DemandaFiltro filtro, CancellationToken ct) =>
        await ExportarAsync(
            () => _relatorios.ExportarDemandasAsync(filtro, ct), "demandas", "Demandas", ct);

    private async Task<IActionResult> ExportarAsync(
        Func<Task<byte[]>> exportacao, string arquivo, string descricao, CancellationToken ct)
    {
        var conteudo = await exportacao();

        await _auditoria.RegistrarESalvarAsync(
            TipoAcaoAuditoria.Alteracao, "Relatorio", null, $"Exportação gerada: {descricao}.", ct);

        return File(conteudo, TipoCsv, $"elcop-{arquivo}-{DateTime.Now:yyyyMMdd-HHmm}.csv");
    }
}
