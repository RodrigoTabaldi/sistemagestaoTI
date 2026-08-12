using Microsoft.AspNetCore.Mvc.Rendering;

namespace Elcop.TI.Web.Models;

/// <summary>
/// Conjunto de listas de seleção alimentado pelos cadastros de apoio.
/// Evita ViewBag nos formulários e mantém a view fortemente tipada.
/// </summary>
public class ListasDeSelecao
{
    public IEnumerable<SelectListItem> Departamentos { get; init; } = Enumerable.Empty<SelectListItem>();

    public IEnumerable<SelectListItem> Localizacoes { get; init; } = Enumerable.Empty<SelectListItem>();

    public IEnumerable<SelectListItem> Fornecedores { get; init; } = Enumerable.Empty<SelectListItem>();

    public IEnumerable<SelectListItem> Colaboradores { get; init; } = Enumerable.Empty<SelectListItem>();

    public IEnumerable<SelectListItem> Ativos { get; init; } = Enumerable.Empty<SelectListItem>();

    public IEnumerable<SelectListItem> Responsaveis { get; init; } = Enumerable.Empty<SelectListItem>();
}
