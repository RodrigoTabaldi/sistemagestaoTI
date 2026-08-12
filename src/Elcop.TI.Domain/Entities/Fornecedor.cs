using System.ComponentModel.DataAnnotations;
using Elcop.TI.Domain.Common;

namespace Elcop.TI.Domain.Entities;

/// <summary>
/// Fornecedor / prestador vinculado à aquisição ou manutenção dos ativos.
/// </summary>
public class Fornecedor : EntidadeBase
{
    [Required(ErrorMessage = "Informe a razão social do fornecedor.")]
    [Display(Name = "Razão social")]
    [StringLength(160)]
    public string Nome { get; set; } = string.Empty;

    [Display(Name = "CNPJ")]
    [StringLength(18)]
    public string? Cnpj { get; set; }

    [Display(Name = "Contato")]
    [StringLength(120)]
    public string? Contato { get; set; }

    [Display(Name = "Telefone")]
    [StringLength(20)]
    public string? Telefone { get; set; }

    [Display(Name = "E-mail")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    [StringLength(160)]
    public string? Email { get; set; }

    [Display(Name = "Observações")]
    [StringLength(1000)]
    public string? Observacoes { get; set; }

    [Display(Name = "Ativo")]
    public bool Habilitado { get; set; } = true;

    public ICollection<Ativo> Ativos { get; set; } = new List<Ativo>();
}
