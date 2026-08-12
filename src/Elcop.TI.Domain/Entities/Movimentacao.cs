using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Elcop.TI.Domain.Common;
using Elcop.TI.Domain.Enums;

namespace Elcop.TI.Domain.Entities;

/// <summary>
/// Termo de responsabilidade: registra a retirada de um ativo por um colaborador e,
/// posteriormente, a sua devolução. É o histórico imutável de posse do equipamento.
/// </summary>
public class Movimentacao : EntidadeBase
{
    [Display(Name = "Protocolo")]
    [StringLength(30)]
    public string Protocolo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Selecione o ativo.")]
    [Display(Name = "Ativo")]
    public int AtivoId { get; set; }

    public Ativo? Ativo { get; set; }

    [Required(ErrorMessage = "Selecione o colaborador.")]
    [Display(Name = "Colaborador")]
    public int ColaboradorId { get; set; }

    public Colaborador? Colaborador { get; set; }

    [Display(Name = "Tipo de movimentação")]
    public TipoMovimentacao Tipo { get; set; } = TipoMovimentacao.Entrega;

    [Display(Name = "Situação")]
    public StatusMovimentacao Status { get; set; } = StatusMovimentacao.EmAberto;

    // ---------- Retirada ----------

    [Required(ErrorMessage = "Informe a data de retirada.")]
    [Display(Name = "Data da retirada")]
    public DateTime DataRetirada { get; set; } = DateTime.Now;

    [Display(Name = "Previsão de devolução")]
    [DataType(DataType.Date)]
    public DateTime? DataPrevistaDevolucao { get; set; }

    [Display(Name = "Condição na retirada")]
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

    [Display(Name = "Observações da retirada")]
    [StringLength(2000)]
    public string? ObservacoesRetirada { get; set; }

    [Display(Name = "Termo de responsabilidade assinado")]
    public bool TermoAssinado { get; set; }

    // ---------- Devolução ----------

    [Display(Name = "Data da devolução")]
    public DateTime? DataDevolucao { get; set; }

    [Display(Name = "Condição na devolução")]
    public CondicaoAtivo? CondicaoDevolucao { get; set; }

    [Display(Name = "Acessórios devolvidos")]
    [StringLength(500)]
    public string? AcessoriosDevolvidos { get; set; }

    [Display(Name = "Responsável pelo recebimento")]
    [StringLength(160)]
    public string? ResponsavelRecebimento { get; set; }

    [Display(Name = "Observações da devolução")]
    [StringLength(2000)]
    public string? ObservacoesDevolucao { get; set; }

    [Display(Name = "Houve avaria na devolução")]
    public bool ComAvaria { get; set; }

    // ---------- Derivadas ----------

    [NotMapped]
    public bool EstaEmAberto => Status is StatusMovimentacao.EmAberto or StatusMovimentacao.Atrasado;

    [NotMapped]
    public bool EstaAtrasada =>
        EstaEmAberto
        && DataPrevistaDevolucao.HasValue
        && DataPrevistaDevolucao.Value.Date < DateTime.Today;

    [NotMapped]
    public int DiasEmPosse =>
        (int)((DataDevolucao ?? DateTime.Now).Date - DataRetirada.Date).TotalDays;

    [NotMapped]
    public int? DiasDeAtraso => EstaAtrasada && DataPrevistaDevolucao.HasValue
        ? (int)(DateTime.Today - DataPrevistaDevolucao.Value.Date).TotalDays
        : null;

    /// <summary>Fecha o ciclo de posse aplicando os dados informados na devolução.</summary>
    public void RegistrarDevolucao(
        DateTime dataDevolucao,
        CondicaoAtivo condicao,
        string? responsavel,
        string? acessorios,
        string? observacoes,
        bool comAvaria)
    {
        DataDevolucao = dataDevolucao;
        CondicaoDevolucao = condicao;
        ResponsavelRecebimento = responsavel;
        AcessoriosDevolvidos = acessorios;
        ObservacoesDevolucao = observacoes;
        ComAvaria = comAvaria;
        Status = StatusMovimentacao.Devolvido;
    }
}
