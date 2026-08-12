using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Elcop.TI.Domain.Common;
using Elcop.TI.Domain.Enums;

namespace Elcop.TI.Domain.Entities;

/// <summary>
/// Chamado / tarefa de TI acompanhado pelo quadro de demandas. O andamento é sempre
/// registrado em <see cref="Andamentos"/>, preservando a linha do tempo do atendimento.
/// </summary>
public class Demanda : EntidadeBase
{
    [Display(Name = "Código")]
    [StringLength(30)]
    public string Codigo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o título da demanda.")]
    [Display(Name = "Título")]
    [StringLength(180, MinimumLength = 5)]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Descreva a demanda.")]
    [Display(Name = "Descrição")]
    [StringLength(5000)]
    public string Descricao { get; set; } = string.Empty;

    [Display(Name = "Categoria")]
    public CategoriaDemanda Categoria { get; set; } = CategoriaDemanda.Suporte;

    [Display(Name = "Prioridade")]
    public PrioridadeDemanda Prioridade { get; set; } = PrioridadeDemanda.Media;

    [Display(Name = "Status")]
    public StatusDemanda Status { get; set; } = StatusDemanda.Aberta;

    [Display(Name = "Solicitante")]
    public int? SolicitanteId { get; set; }

    public Colaborador? Solicitante { get; set; }

    [Display(Name = "Departamento")]
    public int? DepartamentoId { get; set; }

    public Departamento? Departamento { get; set; }

    [Display(Name = "Ativo relacionado")]
    public int? AtivoId { get; set; }

    public Ativo? Ativo { get; set; }

    [Display(Name = "Responsável")]
    [StringLength(160)]
    public string? Responsavel { get; set; }

    [Display(Name = "Abertura")]
    public DateTime DataAbertura { get; set; } = DateTime.Now;

    [Display(Name = "Prazo (SLA)")]
    [DataType(DataType.Date)]
    public DateTime? PrazoLimite { get; set; }

    [Display(Name = "Início do atendimento")]
    public DateTime? DataInicio { get; set; }

    [Display(Name = "Conclusão")]
    public DateTime? DataConclusao { get; set; }

    [Display(Name = "Progresso (%)")]
    [Range(0, 100, ErrorMessage = "O progresso deve estar entre 0 e 100.")]
    public int PercentualConclusao { get; set; }

    [Display(Name = "Tempo gasto (min)")]
    public int TempoGastoMinutos { get; set; }

    [Display(Name = "Ordem no quadro")]
    public int Ordem { get; set; }

    [Display(Name = "Etiquetas")]
    [StringLength(300)]
    public string? Tags { get; set; }

    [Display(Name = "Solução aplicada")]
    [StringLength(5000)]
    public string? Solucao { get; set; }

    public ICollection<DemandaAndamento> Andamentos { get; set; } = new List<DemandaAndamento>();

    // ---------- Derivadas ----------

    [NotMapped]
    public bool Encerrada => Status is StatusDemanda.Concluida or StatusDemanda.Cancelada;

    [NotMapped]
    public bool Atrasada =>
        !Encerrada && PrazoLimite.HasValue && PrazoLimite.Value.Date < DateTime.Today;

    [NotMapped]
    public int? DiasRestantes => PrazoLimite.HasValue && !Encerrada
        ? (int)(PrazoLimite.Value.Date - DateTime.Today).TotalDays
        : null;

    [NotMapped]
    public string TempoGastoFormatado =>
        TempoGastoMinutos <= 0
            ? "—"
            : $"{TempoGastoMinutos / 60}h {TempoGastoMinutos % 60:00}min";

    [NotMapped]
    public IEnumerable<string> ListaTags =>
        string.IsNullOrWhiteSpace(Tags)
            ? Array.Empty<string>()
            : Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>Prazo sugerido conforme a prioridade, usado quando o usuário não define um SLA.</summary>
    public static DateTime CalcularPrazoSugerido(PrioridadeDemanda prioridade, DateTime referencia) =>
        prioridade switch
        {
            PrioridadeDemanda.Critica => referencia.AddHours(4),
            PrioridadeDemanda.Alta => referencia.AddDays(1),
            PrioridadeDemanda.Media => referencia.AddDays(3),
            _ => referencia.AddDays(7)
        };

    /// <summary>
    /// Aplica a transição de status mantendo coerência entre datas e percentual.
    /// </summary>
    public void AplicarStatus(StatusDemanda novoStatus)
    {
        if (novoStatus == StatusDemanda.EmAndamento && DataInicio is null)
            DataInicio = DateTime.Now;

        if (novoStatus == StatusDemanda.Concluida)
        {
            DataConclusao = DateTime.Now;
            PercentualConclusao = 100;
        }
        else if (Status == StatusDemanda.Concluida && novoStatus != StatusDemanda.Concluida)
        {
            // Reabertura: limpa a conclusão para o SLA voltar a contar.
            DataConclusao = null;
            if (PercentualConclusao == 100) PercentualConclusao = 90;
        }

        if (novoStatus == StatusDemanda.Cancelada)
            DataConclusao = DateTime.Now;

        Status = novoStatus;
    }
}
