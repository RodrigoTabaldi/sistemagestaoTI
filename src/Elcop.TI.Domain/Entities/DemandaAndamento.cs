using System.ComponentModel.DataAnnotations;
using Elcop.TI.Domain.Common;
using Elcop.TI.Domain.Enums;

namespace Elcop.TI.Domain.Entities;

/// <summary>
/// Entrada da linha do tempo de uma demanda: comentário, apontamento de horas
/// e/ou registro de mudança de status.
/// </summary>
public class DemandaAndamento : EntidadeBase
{
    [Display(Name = "Demanda")]
    public int DemandaId { get; set; }

    public Demanda? Demanda { get; set; }

    [Required(ErrorMessage = "Descreva o andamento.")]
    [Display(Name = "Andamento")]
    [StringLength(4000)]
    public string Descricao { get; set; } = string.Empty;

    [Display(Name = "Autor")]
    [StringLength(160)]
    public string? Autor { get; set; }

    [Display(Name = "Data")]
    public DateTime Data { get; set; } = DateTime.Now;

    [Display(Name = "Status anterior")]
    public StatusDemanda? StatusAnterior { get; set; }

    [Display(Name = "Novo status")]
    public StatusDemanda? StatusNovo { get; set; }

    [Display(Name = "Tempo gasto (min)")]
    [Range(0, 10_000, ErrorMessage = "Informe um tempo entre 0 e 10.000 minutos.")]
    public int TempoGastoMinutos { get; set; }

    [Display(Name = "Progresso informado (%)")]
    [Range(0, 100)]
    public int? PercentualInformado { get; set; }

    /// <summary>Marca entradas geradas pelo próprio sistema (mudança de coluna no kanban, etc.).</summary>
    [Display(Name = "Registro automático")]
    public bool Automatico { get; set; }

    public bool HouveMudancaDeStatus => StatusNovo.HasValue && StatusNovo != StatusAnterior;
}
