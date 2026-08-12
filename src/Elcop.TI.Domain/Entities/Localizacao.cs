using System.ComponentModel.DataAnnotations;
using Elcop.TI.Domain.Common;

namespace Elcop.TI.Domain.Entities;

/// <summary>
/// Unidade física / sala / almoxarifado onde o ativo se encontra.
/// </summary>
public class Localizacao : EntidadeBase
{
    [Required(ErrorMessage = "Informe o nome da localização.")]
    [Display(Name = "Localização")]
    [StringLength(120)]
    public string Nome { get; set; } = string.Empty;

    [Display(Name = "Unidade / Filial")]
    [StringLength(120)]
    public string? Unidade { get; set; }

    [Display(Name = "Endereço")]
    [StringLength(200)]
    public string? Endereco { get; set; }

    [Display(Name = "Cidade")]
    [StringLength(80)]
    public string? Cidade { get; set; }

    [Display(Name = "UF")]
    [StringLength(2)]
    public string? Uf { get; set; }

    [Display(Name = "Ativo")]
    public bool Habilitado { get; set; } = true;

    public ICollection<Ativo> Ativos { get; set; } = new List<Ativo>();
}
