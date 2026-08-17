using Elcop.TI.Application.Services;
using Elcop.TI.Domain.Enums;
using Elcop.TI.Web.Infra;
using Elcop.TI.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elcop.TI.Web.Controllers;

/// <summary>Consulta da trilha de auditoria do sistema.</summary>
[Authorize(Policy = Politicas.Administrar)]
public class AuditoriaController : Controller
{
    private readonly IAuditoriaService _auditoria;

    public AuditoriaController(IAuditoriaService auditoria) => _auditoria = auditoria;

    public async Task<IActionResult> Index(
        string? busca,
        TipoAcaoAuditoria? acao,
        DateTime? de,
        DateTime? ate,
        int pagina = 1,
        CancellationToken ct = default) =>
        View(new AuditoriaViewModel
        {
            Pagina = await _auditoria.ListarAsync(busca, acao, de, ate, pagina, 30, ct),
            Busca = busca,
            Acao = acao,
            De = de,
            Ate = ate
        });
}
