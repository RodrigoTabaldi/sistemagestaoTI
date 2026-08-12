using System.ComponentModel.DataAnnotations;
using Elcop.TI.Domain.Common;

namespace Elcop.TI.Domain.Entities;

/// <summary>
/// Setor da empresa ao qual colaboradores, ativos e demandas são vinculados.
/// </summary>
public class Departamento : EntidadeBase
{
    [Required(ErrorMessage = "Informe o nome do departamento.")]
    [Display(Name = "Departamento")]
    [StringLength(120)]
    public string Nome { get; set; } = string.Empty;

    [Display(Name = "Sigla")]
    [StringLength(20)]
    public string? Sigla { get; set; }

    [Display(Name = "Centro de custo")]
    [StringLength(40)]
    public string? CentroCusto { get; set; }

    [Display(Name = "Responsável")]
    [StringLength(120)]
    public string? Responsavel { get; set; }

    [Display(Name = "Ativo")]
    public bool Habilitado { get; set; } = true;

    public ICollection<Colaborador> Colaboradores { get; set; } = new List<Colaborador>();

    public ICollection<Ativo> Ativos { get; set; } = new List<Ativo>();
}
