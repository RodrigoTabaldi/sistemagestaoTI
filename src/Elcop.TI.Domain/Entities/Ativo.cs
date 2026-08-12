using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Elcop.TI.Domain.Common;
using Elcop.TI.Domain.Enums;

namespace Elcop.TI.Domain.Entities;

/// <summary>
/// Equipamento de TI controlado pelo inventário. Concentra dados de identificação,
/// aquisição, especificação técnica e a posse corrente (derivada da última movimentação).
/// </summary>
public class Ativo : EntidadeBase
{
    // ---------- Identificação ----------

    [Required(ErrorMessage = "Informe o número de patrimônio.")]
    [Display(Name = "Patrimônio")]
    [StringLength(40)]
    public string Patrimonio { get; set; } = string.Empty;

    [Required(ErrorMessage = "Selecione o tipo do ativo.")]
    [Display(Name = "Tipo de ativo")]
    public TipoAtivo Tipo { get; set; }

    [Required(ErrorMessage = "Informe a marca.")]
    [Display(Name = "Marca / Fabricante")]
    [StringLength(80)]
    public string Marca { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o modelo.")]
    [Display(Name = "Modelo")]
    [StringLength(120)]
    public string Modelo { get; set; } = string.Empty;

    [Display(Name = "Número de série")]
    [StringLength(80)]
    public string? NumeroSerie { get; set; }

    [Display(Name = "IMEI")]
    [StringLength(20)]
    [RegularExpression(@"^\d{14,17}$", ErrorMessage = "O IMEI deve conter de 14 a 17 dígitos.")]
    public string? Imei { get; set; }

    [Display(Name = "IMEI secundário")]
    [StringLength(20)]
    [RegularExpression(@"^\d{14,17}$", ErrorMessage = "O IMEI deve conter de 14 a 17 dígitos.")]
    public string? Imei2 { get; set; }

    [Display(Name = "Linha / Número")]
    [StringLength(30)]
    public string? NumeroLinha { get; set; }

    [Display(Name = "Operadora")]
    [StringLength(60)]
    public string? Operadora { get; set; }

    [Display(Name = "Endereço MAC")]
    [StringLength(20)]
    public string? EnderecoMac { get; set; }

    [Display(Name = "Cor")]
    [StringLength(40)]
    public string? Cor { get; set; }

    [Display(Name = "Etiqueta / Tombamento")]
    [StringLength(60)]
    public string? Etiqueta { get; set; }

    // ---------- Situação ----------

    [Display(Name = "Status")]
    public StatusAtivo Status { get; set; } = StatusAtivo.Disponivel;

    [Display(Name = "Condição")]
    public CondicaoAtivo Condicao { get; set; } = CondicaoAtivo.Novo;

    [Display(Name = "Departamento")]
    public int? DepartamentoId { get; set; }

    public Departamento? Departamento { get; set; }

    [Display(Name = "Localização")]
    public int? LocalizacaoId { get; set; }

    public Localizacao? Localizacao { get; set; }

    /// <summary>Colaborador que está com o ativo no momento (null quando em estoque).</summary>
    [Display(Name = "Em posse de")]
    public int? ColaboradorAtualId { get; set; }

    public Colaborador? ColaboradorAtual { get; set; }

    // ---------- Aquisição ----------

    [Display(Name = "Fornecedor")]
    public int? FornecedorId { get; set; }

    public Fornecedor? Fornecedor { get; set; }

    [Display(Name = "Data de aquisição")]
    [DataType(DataType.Date)]
    public DateTime? DataAquisicao { get; set; }

    [Display(Name = "Valor de aquisição")]
    [DataType(DataType.Currency)]
    [Range(0, 9_999_999, ErrorMessage = "Valor inválido.")]
    public decimal? ValorAquisicao { get; set; }

    [Display(Name = "Nota fiscal")]
    [StringLength(60)]
    public string? NotaFiscal { get; set; }

    [Display(Name = "Garantia até")]
    [DataType(DataType.Date)]
    public DateTime? GarantiaAte { get; set; }

    [Display(Name = "Contrato / Locação")]
    [StringLength(80)]
    public string? Contrato { get; set; }

    // ---------- Especificações técnicas ----------

    [Display(Name = "Processador")]
    [StringLength(120)]
    public string? Processador { get; set; }

    [Display(Name = "Memória RAM")]
    [StringLength(60)]
    public string? MemoriaRam { get; set; }

    [Display(Name = "Armazenamento")]
    [StringLength(60)]
    public string? Armazenamento { get; set; }

    [Display(Name = "Sistema operacional")]
    [StringLength(80)]
    public string? SistemaOperacional { get; set; }

    [Display(Name = "Nome na rede (hostname)")]
    [StringLength(80)]
    public string? Hostname { get; set; }

    [Display(Name = "Polegadas / Tamanho")]
    [StringLength(30)]
    public string? Tamanho { get; set; }

    [Display(Name = "Acessórios que acompanham")]
    [StringLength(500)]
    public string? Acessorios { get; set; }

    [Display(Name = "Observações")]
    [StringLength(2000)]
    public string? Observacoes { get; set; }

    [Display(Name = "Foto do ativo")]
    [StringLength(300)]
    public string? FotoUrl { get; set; }

    public ICollection<Movimentacao> Movimentacoes { get; set; } = new List<Movimentacao>();

    public ICollection<Demanda> Demandas { get; set; } = new List<Demanda>();

    // ---------- Propriedades derivadas ----------

    [NotMapped]
    public string DescricaoCurta => $"{Marca} {Modelo}".Trim();

    [NotMapped]
    public string Identificacao => string.IsNullOrWhiteSpace(NumeroSerie)
        ? Patrimonio
        : $"{Patrimonio} · SN {NumeroSerie}";

    /// <summary>Só é possível entregar um ativo que esteja livre e utilizável.</summary>
    [NotMapped]
    public bool DisponivelParaEntrega =>
        Status is StatusAtivo.Disponivel or StatusAtivo.Reservado && !Excluido;

    [NotMapped]
    public bool EmGarantia => GarantiaAte.HasValue && GarantiaAte.Value.Date >= DateTime.Today;

    [NotMapped]
    public bool GarantiaProximaDoVencimento =>
        GarantiaAte.HasValue
        && GarantiaAte.Value.Date >= DateTime.Today
        && GarantiaAte.Value.Date <= DateTime.Today.AddDays(60);

    /// <summary>Tipos em que IMEI/linha fazem sentido no formulário.</summary>
    [NotMapped]
    public bool ExigeIdentificacaoMovel =>
        Tipo is TipoAtivo.Celular or TipoAtivo.Tablet or TipoAtivo.Bodycam or TipoAtivo.ChipLinhaMovel;

    /// <summary>Tipos em que especificação de hardware faz sentido no formulário.</summary>
    [NotMapped]
    public bool ExigeEspecificacaoTecnica =>
        Tipo is TipoAtivo.Notebook or TipoAtivo.Desktop or TipoAtivo.Servidor or TipoAtivo.Tablet;

    [NotMapped]
    public int IdadeEmMeses => DataAquisicao.HasValue
        ? (int)Math.Max(0, (DateTime.Today - DataAquisicao.Value.Date).TotalDays / 30.44)
        : 0;
}
