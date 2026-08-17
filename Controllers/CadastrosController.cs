using Elcop.TI.Application.Services;
using Elcop.TI.Domain.Entities;
using Elcop.TI.Web.Infra;
using Elcop.TI.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elcop.TI.Web.Controllers;

/// <summary>Cadastros de apoio: departamentos, localizações e fornecedores.</summary>
[Authorize(Policy = Politicas.Administrar)]
public class CadastrosController : Controller
{
    private readonly ICadastroService _cadastros;

    public CadastrosController(ICadastroService cadastros) => _cadastros = cadastros;

    public async Task<IActionResult> Index(string aba = "departamentos", CancellationToken ct = default) =>
        View(new CadastrosViewModel
        {
            Departamentos = await _cadastros.ListarDepartamentosAsync(ct: ct),
            Localizacoes = await _cadastros.ListarLocalizacoesAsync(ct: ct),
            Fornecedores = await _cadastros.ListarFornecedoresAsync(ct: ct),
            AbaAtiva = aba
        });

    // ------------------------------------------------------------- Departamentos

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SalvarDepartamento(Departamento departamento, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            this.NotificarErro("Verifique os campos do departamento.");
            return RedirectToAction(nameof(Index), new { aba = "departamentos" });
        }

        await _cadastros.SalvarDepartamentoAsync(departamento, ct);
        this.NotificarSucesso("Departamento salvo.");

        return RedirectToAction(nameof(Index), new { aba = "departamentos" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExcluirDepartamento(int id, CancellationToken ct)
    {
        await _cadastros.ExcluirDepartamentoAsync(id, ct);
        this.NotificarSucesso("Departamento excluído.");

        return RedirectToAction(nameof(Index), new { aba = "departamentos" });
    }

    // ------------------------------------------------------------- Localizações

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SalvarLocalizacao(Localizacao localizacao, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            this.NotificarErro("Verifique os campos da localização.");
            return RedirectToAction(nameof(Index), new { aba = "localizacoes" });
        }

        await _cadastros.SalvarLocalizacaoAsync(localizacao, ct);
        this.NotificarSucesso("Localização salva.");

        return RedirectToAction(nameof(Index), new { aba = "localizacoes" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExcluirLocalizacao(int id, CancellationToken ct)
    {
        await _cadastros.ExcluirLocalizacaoAsync(id, ct);
        this.NotificarSucesso("Localização excluída.");

        return RedirectToAction(nameof(Index), new { aba = "localizacoes" });
    }

    // ------------------------------------------------------------- Fornecedores

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SalvarFornecedor(Fornecedor fornecedor, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            this.NotificarErro("Verifique os campos do fornecedor.");
            return RedirectToAction(nameof(Index), new { aba = "fornecedores" });
        }

        await _cadastros.SalvarFornecedorAsync(fornecedor, ct);
        this.NotificarSucesso("Fornecedor salvo.");

        return RedirectToAction(nameof(Index), new { aba = "fornecedores" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExcluirFornecedor(int id, CancellationToken ct)
    {
        await _cadastros.ExcluirFornecedorAsync(id, ct);
        this.NotificarSucesso("Fornecedor excluído.");

        return RedirectToAction(nameof(Index), new { aba = "fornecedores" });
    }
}
