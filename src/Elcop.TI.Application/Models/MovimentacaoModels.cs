using System.ComponentModel.DataAnnotations;
using Elcop.TI.Domain.Enums;

namespace Elcop.TI.Application.Models;

/// <summary>
/// Dados coletados no ato da <b>retirada</b> do ativo pelo colaborador.
/// </summary>
public class EntregaAtivoModel : IValidatableObject
{
    [Required(ErrorMessage = "Selecione o ativo que será entregue.")]
    [Display(Name = "Ativo")]
    public int AtivoId { get; set; }

    [Required(ErrorMessage = "Selecione o colaborador que está retirando.")]
    [Display(Name = "Colaborador que está retirando")]
    public int ColaboradorId { get; set; }

    [Display(Name = "Tipo de movimentação")]
    public TipoMovimentacao Tipo { get; set; } = TipoMovimentacao.Entrega;

    [Required(ErrorMessage = "Informe a data e hora da retirada.")]
    [Display(Name = "Data/hora da retirada")]
    public DateTime DataRetirada { get; set; } = DateTime.Now;

    [Display(Name = "Previsão de devolução")]
    [DataType(DataType.Date)]
    public DateTime? DataPrevistaDevolucao { get; set; }

    [Display(Name = "Condição do equipamento na retirada")]
    public CondicaoAtivo CondicaoRetirada { get; set; } = CondicaoAtivo.Bom;

    [Display(Name = "Acessórios entregues")]
    [StringLength(500)]
    public string? AcessoriosEntregues { get; set; }

    [Display(Name = "Responsável pela entrega")]
    [StringLength(160)]
    public string? ResponsavelEntrega { get; set; }

    [Display(Name = "Local da entrega")]
    [StringLength(160)]
    public string? LocalEntrega { get; set; }

    [Display(Name = "Observações")]
    [StringLength(2000)]
    public string? ObservacoesRetirada { get; set; }

    [Display(Name = "Termo de responsabilidade assinado pelo colaborador")]
    public bool TermoAssinado { get; set; }

    /// <summary>Atualiza o departamento/localização do ativo com os do colaborador.</summary>
    [Display(Name = "Herdar departamento e localização do colaborador")]
    public bool HerdarLotacao { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DataRetirada > DateTime.Now.AddMinutes(5))
            yield return new ValidationResult(
                "A data de retirada não pode estar no futuro.", new[] { nameof(DataRetirada) });

        if (DataPrevistaDevolucao.HasValue && DataPrevistaDevolucao.Value.Date < DataRetirada.Date)
            yield return new ValidationResult(
                "A previsão de devolução deve ser posterior à retirada.",
                new[] { nameof(DataPrevistaDevolucao) });

        if (Tipo == TipoMovimentacao.Emprestimo && !DataPrevistaDevolucao.HasValue)
            yield return new ValidationResult(
                "Empréstimos temporários exigem uma previsão de devolução.",
                new[] { nameof(DataPrevistaDevolucao) });
    }
}

/// <summary>
/// Dados coletados no ato da <b>devolução</b> do ativo ao estoque de TI.
/// </summary>
public class DevolucaoAtivoModel : IValidatableObject
{
    [Display(Name = "Movimentação")]
    public int MovimentacaoId { get; set; }

    [Required(ErrorMessage = "Informe a data e hora da devolução.")]
    [Display(Name = "Data/hora da devolução")]
    public DateTime DataDevolucao { get; set; } = DateTime.Now;

    [Display(Name = "Condição do equipamento na devolução")]
    public CondicaoAtivo CondicaoDevolucao { get; set; } = CondicaoAtivo.Bom;

    [Display(Name = "Acessórios devolvidos")]
    [StringLength(500)]
    public string? AcessoriosDevolvidos { get; set; }

    [Display(Name = "Responsável pelo recebimento")]
    [StringLength(160)]
    public string? ResponsavelRecebimento { get; set; }

    [Display(Name = "Observações da devolução")]
    [StringLength(2000)]
    public string? ObservacoesDevolucao { get; set; }

    [Display(Name = "Equipamento devolvido com avaria")]
    public bool ComAvaria { get; set; }

    [Display(Name = "Novo status do ativo")]
    public StatusAtivo StatusDestino { get; set; } = StatusAtivo.Disponivel;

    /// <summary>Data de retirada, apenas para exibição e validação no formulário.</summary>
    public DateTime DataRetiradaOriginal { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DataDevolucao > DateTime.Now.AddMinutes(5))
            yield return new ValidationResult(
                "A data de devolução não pode estar no futuro.", new[] { nameof(DataDevolucao) });

        if (DataRetiradaOriginal != default && DataDevolucao < DataRetiradaOriginal)
            yield return new ValidationResult(
                "A devolução não pode ser anterior à retirada.", new[] { nameof(DataDevolucao) });

        if (ComAvaria && CondicaoDevolucao is CondicaoAtivo.Novo or CondicaoAtivo.Otimo)
            yield return new ValidationResult(
                "Um equipamento devolvido com avaria não pode ser classificado como Novo/Ótimo.",
                new[] { nameof(CondicaoDevolucao) });
    }
}
