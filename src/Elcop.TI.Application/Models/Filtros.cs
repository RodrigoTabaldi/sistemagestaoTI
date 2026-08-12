using System.ComponentModel.DataAnnotations;
using Elcop.TI.Domain.Enums;

namespace Elcop.TI.Application.Models;

/// <summary>Parâmetros comuns de busca, ordenação e paginação das listagens.</summary>
public abstract class FiltroBase
{
    [Display(Name = "Buscar")]
    public string? Busca { get; set; }

    [Display(Name = "Ordenar por")]
    public string? Ordenacao { get; set; }

    public int Pagina { get; set; } = 1;

    public int TamanhoPagina { get; set; } = 20;

    /// <summary>Usado nas views para decidir se o painel de filtros abre expandido.</summary>
    public virtual bool PossuiFiltroAtivo => !string.IsNullOrWhiteSpace(Busca);

    protected static string? Limpar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}

public class AtivoFiltro : FiltroBase
{
    [Display(Name = "Tipo")]
    public TipoAtivo? Tipo { get; set; }

    [Display(Name = "Status")]
    public StatusAtivo? Status { get; set; }

    [Display(Name = "Condição")]
    public CondicaoAtivo? Condicao { get; set; }

    [Display(Name = "Departamento")]
    public int? DepartamentoId { get; set; }

    [Display(Name = "Localização")]
    public int? LocalizacaoId { get; set; }

    [Display(Name = "Fornecedor")]
    public int? FornecedorId { get; set; }

    [Display(Name = "Colaborador")]
    public int? ColaboradorId { get; set; }

    [Display(Name = "Somente com garantia vencendo (60 dias)")]
    public bool GarantiaVencendo { get; set; }

    [Display(Name = "Somente sem número de série")]
    public bool SemNumeroSerie { get; set; }

    public override bool PossuiFiltroAtivo =>
        base.PossuiFiltroAtivo || Tipo.HasValue || Status.HasValue || Condicao.HasValue
        || DepartamentoId.HasValue || LocalizacaoId.HasValue || FornecedorId.HasValue
        || ColaboradorId.HasValue || GarantiaVencendo || SemNumeroSerie;
}

public class ColaboradorFiltro : FiltroBase
{
    [Display(Name = "Departamento")]
    public int? DepartamentoId { get; set; }

    [Display(Name = "Situação")]
    public StatusColaborador? Status { get; set; }

    [Display(Name = "Somente com ativos em posse")]
    public bool ComAtivos { get; set; }

    public override bool PossuiFiltroAtivo =>
        base.PossuiFiltroAtivo || DepartamentoId.HasValue || Status.HasValue || ComAtivos;
}

public class MovimentacaoFiltro : FiltroBase
{
    [Display(Name = "Tipo")]
    public TipoMovimentacao? Tipo { get; set; }

    [Display(Name = "Situação")]
    public StatusMovimentacao? Status { get; set; }

    [Display(Name = "Colaborador")]
    public int? ColaboradorId { get; set; }

    [Display(Name = "Ativo")]
    public int? AtivoId { get; set; }

    [Display(Name = "De")]
    [DataType(DataType.Date)]
    public DateTime? DataInicial { get; set; }

    [Display(Name = "Até")]
    [DataType(DataType.Date)]
    public DateTime? DataFinal { get; set; }

    [Display(Name = "Somente devoluções em atraso")]
    public bool SomenteAtrasadas { get; set; }

    public override bool PossuiFiltroAtivo =>
        base.PossuiFiltroAtivo || Tipo.HasValue || Status.HasValue || ColaboradorId.HasValue
        || AtivoId.HasValue || DataInicial.HasValue || DataFinal.HasValue || SomenteAtrasadas;
}

public class DemandaFiltro : FiltroBase
{
    [Display(Name = "Status")]
    public StatusDemanda? Status { get; set; }

    [Display(Name = "Prioridade")]
    public PrioridadeDemanda? Prioridade { get; set; }

    [Display(Name = "Categoria")]
    public CategoriaDemanda? Categoria { get; set; }

    [Display(Name = "Departamento")]
    public int? DepartamentoId { get; set; }

    [Display(Name = "Solicitante")]
    public int? SolicitanteId { get; set; }

    [Display(Name = "Responsável")]
    public string? Responsavel { get; set; }

    [Display(Name = "Somente atrasadas")]
    public bool SomenteAtrasadas { get; set; }

    [Display(Name = "Incluir encerradas")]
    public bool IncluirEncerradas { get; set; } = true;

    public override bool PossuiFiltroAtivo =>
        base.PossuiFiltroAtivo || Status.HasValue || Prioridade.HasValue || Categoria.HasValue
        || DepartamentoId.HasValue || SolicitanteId.HasValue
        || !string.IsNullOrWhiteSpace(Responsavel) || SomenteAtrasadas || !IncluirEncerradas;
}
