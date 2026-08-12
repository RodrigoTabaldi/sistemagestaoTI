using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Elcop.TI.Domain.Common;
using Elcop.TI.Domain.Enums;

namespace Elcop.TI.Domain.Entities;

/// <summary>
/// Pessoa da empresa que pode receber ativos em posse e abrir demandas de TI.
/// </summary>
public class Colaborador : EntidadeBase
{
    [Required(ErrorMessage = "Informe o nome completo.")]
    [Display(Name = "Nome completo")]
    [StringLength(160, MinimumLength = 3)]
    public string NomeCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a matrícula.")]
    [Display(Name = "Matrícula")]
    [StringLength(30)]
    public string Matricula { get; set; } = string.Empty;

    [Display(Name = "CPF")]
    [StringLength(14)]
    public string? Cpf { get; set; }

    [Display(Name = "RG")]
    [StringLength(20)]
    public string? Rg { get; set; }

    [Required(ErrorMessage = "Informe o e-mail corporativo.")]
    [Display(Name = "E-mail corporativo")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    [StringLength(160)]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "E-mail pessoal")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    [StringLength(160)]
    public string? EmailPessoal { get; set; }

    [Display(Name = "Telefone / Ramal")]
    [StringLength(30)]
    public string? Telefone { get; set; }

    [Display(Name = "Celular")]
    [StringLength(30)]
    public string? Celular { get; set; }

    [Display(Name = "Cargo")]
    [StringLength(120)]
    public string? Cargo { get; set; }

    [Display(Name = "Gestor imediato")]
    [StringLength(160)]
    public string? GestorImediato { get; set; }

    [Display(Name = "Departamento")]
    public int? DepartamentoId { get; set; }

    public Departamento? Departamento { get; set; }

    [Display(Name = "Localização")]
    public int? LocalizacaoId { get; set; }

    public Localizacao? Localizacao { get; set; }

    [Display(Name = "Situação")]
    public StatusColaborador Status { get; set; } = StatusColaborador.Ativo;

    [Display(Name = "Data de admissão")]
    [DataType(DataType.Date)]
    public DateTime? DataAdmissao { get; set; }

    [Display(Name = "Data de desligamento")]
    [DataType(DataType.Date)]
    public DateTime? DataDesligamento { get; set; }

    [Display(Name = "Observações")]
    [StringLength(2000)]
    public string? Observacoes { get; set; }

    [Display(Name = "Foto")]
    [StringLength(300)]
    public string? FotoUrl { get; set; }

    public ICollection<Movimentacao> Movimentacoes { get; set; } = new List<Movimentacao>();

    public ICollection<Demanda> DemandasSolicitadas { get; set; } = new List<Demanda>();

    /// <summary>Iniciais usadas no avatar quando não há foto cadastrada.</summary>
    [NotMapped]
    public string Iniciais
    {
        get
        {
            var partes = NomeCompleto.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length == 0) return "?";
            if (partes.Length == 1) return partes[0][..1].ToUpperInvariant();
            return string.Concat(partes[0][..1], partes[^1][..1]).ToUpperInvariant();
        }
    }

    [NotMapped]
    public string PrimeiroNome =>
        NomeCompleto.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? NomeCompleto;

    /// <summary>Somente colaboradores ativos ou de férias podem receber novos ativos.</summary>
    [NotMapped]
    public bool PodeReceberAtivos => Status is StatusColaborador.Ativo or StatusColaborador.Ferias;
}
