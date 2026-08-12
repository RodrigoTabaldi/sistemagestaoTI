using Elcop.TI.Application.Services;
using Elcop.TI.Domain.Common;
using Elcop.TI.Web.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Elcop.TI.Web.Services;

public interface ISelecaoService
{
    Task<ListasDeSelecao> MontarAsync(
        bool colaboradores = false,
        bool ativos = false,
        bool responsaveis = false,
        int? ativoSelecionado = null,
        CancellationToken ct = default);
}

/// <summary>
/// Monta as listas de seleção dos formulários a partir dos cadastros de apoio.
/// Cada lista só é consultada quando o formulário realmente precisa dela.
/// </summary>
public class SelecaoService : ISelecaoService
{
    private readonly ICadastroService _cadastros;
    private readonly IColaboradorService _colaboradores;
    private readonly IAtivoService _ativos;
    private readonly IDemandaService _demandas;

    public SelecaoService(
        ICadastroService cadastros,
        IColaboradorService colaboradores,
        IAtivoService ativos,
        IDemandaService demandas)
    {
        _cadastros = cadastros;
        _colaboradores = colaboradores;
        _ativos = ativos;
        _demandas = demandas;
    }

    public async Task<ListasDeSelecao> MontarAsync(
        bool colaboradores = false,
        bool ativos = false,
        bool responsaveis = false,
        int? ativoSelecionado = null,
        CancellationToken ct = default)
    {
        var departamentos = await _cadastros.ListarDepartamentosAsync(somenteHabilitados: true, ct);
        var localizacoes = await _cadastros.ListarLocalizacoesAsync(somenteHabilitadas: true, ct);
        var fornecedores = await _cadastros.ListarFornecedoresAsync(somenteHabilitados: true, ct);

        var listas = new ListasDeSelecao
        {
            Departamentos = departamentos
                .Select(d => new SelectListItem(d.Sigla is null ? d.Nome : $"{d.Nome} ({d.Sigla})", d.Id.ToString()))
                .ToList(),

            Localizacoes = localizacoes
                .Select(l => new SelectListItem(
                    string.IsNullOrWhiteSpace(l.Unidade) ? l.Nome : $"{l.Unidade} · {l.Nome}", l.Id.ToString()))
                .ToList(),

            Fornecedores = fornecedores
                .Select(f => new SelectListItem(f.Nome, f.Id.ToString()))
                .ToList(),

            Colaboradores = colaboradores
                ? (await _colaboradores.ListarParaSelecaoAsync(ct))
                    .Select(c => new SelectListItem(
                        $"{c.NomeCompleto} · {c.Matricula}" +
                        (c.Departamento is null ? string.Empty : $" · {c.Departamento.Nome}"),
                        c.Id.ToString()))
                    .ToList()
                : Enumerable.Empty<SelectListItem>(),

            Ativos = ativos
                ? (await _ativos.ListarDisponiveisAsync(ativoSelecionado, ct))
                    .Select(a => new SelectListItem(
                        $"{a.Patrimonio} · {a.Tipo.ObterNome()} · {a.DescricaoCurta}" +
                        (string.IsNullOrWhiteSpace(a.NumeroSerie) ? string.Empty : $" · SN {a.NumeroSerie}"),
                        a.Id.ToString()))
                    .ToList()
                : Enumerable.Empty<SelectListItem>(),

            Responsaveis = responsaveis
                ? (await _demandas.ListarResponsaveisAsync(ct))
                    .Select(r => new SelectListItem(r, r))
                    .ToList()
                : Enumerable.Empty<SelectListItem>()
        };

        return listas;
    }
}
