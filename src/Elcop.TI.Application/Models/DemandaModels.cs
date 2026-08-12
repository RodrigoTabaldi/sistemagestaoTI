using System.ComponentModel.DataAnnotations;
using Elcop.TI.Domain.Entities;
using Elcop.TI.Domain.Enums;

namespace Elcop.TI.Application.Models;

/// <summary>Novo apontamento na linha do tempo da demanda.</summary>
public class NovoAndamentoModel
{
    public int DemandaId { get; set; }

    [Required(ErrorMessage = "Descreva o que foi feito.")]
    [Display(Name = "Andamento")]
    [StringLength(4000, MinimumLength = 3)]
    public string Descricao { get; set; } = string.Empty;

    [Display(Name = "Alterar status para")]
    public StatusDemanda? NovoStatus { get; set; }

    [Display(Name = "Tempo gasto (minutos)")]
    [Range(0, 10_000, ErrorMessage = "Informe um tempo entre 0 e 10.000 minutos.")]
    public int TempoGastoMinutos { get; set; }

    [Display(Name = "Progresso (%)")]
    [Range(0, 100)]
    public int? PercentualConclusao { get; set; }
}

/// <summary>Totais exibidos no cabeçalho da tela de demandas.</summary>
public record ContadoresDemanda(int Abertas, int EmAndamento, int Atrasadas, int ConcluidasNoMes, int Criticas);

/// <summary>Uma coluna do quadro kanban de demandas.</summary>
public class ColunaKanban
{
    public StatusDemanda Status { get; init; }

    public string Titulo { get; init; } = string.Empty;

    public IReadOnlyList<Demanda> Demandas { get; init; } = Array.Empty<Demanda>();

    public int Total => Demandas.Count;

    public int Atrasadas => Demandas.Count(d => d.Atrasada);
}

/// <summary>Quadro completo com as colunas de trabalho ativas.</summary>
public class QuadroKanban
{
    public IReadOnlyList<ColunaKanban> Colunas { get; init; } = Array.Empty<ColunaKanban>();

    public DemandaFiltro Filtro { get; init; } = new();

    public int TotalDemandas => Colunas.Sum(c => c.Total);

    public int TotalAtrasadas => Colunas.Sum(c => c.Atrasadas);
}
