using System.ComponentModel.DataAnnotations;
using Elcop.TI.Domain.Enums;

namespace Elcop.TI.Domain.Entities;

/// <summary>
/// Trilha de auditoria imutável — quem fez o quê, quando e em qual registro.
/// </summary>
public class RegistroAuditoria
{
    public int Id { get; set; }

    [Display(Name = "Data/hora")]
    public DateTime DataHora { get; set; } = DateTime.Now;

    [Display(Name = "Usuário")]
    [StringLength(160)]
    public string Usuario { get; set; } = "sistema";

    [Display(Name = "Ação")]
    public TipoAcaoAuditoria Acao { get; set; }

    [Display(Name = "Entidade")]
    [StringLength(80)]
    public string Entidade { get; set; } = string.Empty;

    [Display(Name = "Registro")]
    public int? EntidadeId { get; set; }

    [Display(Name = "Descrição")]
    [StringLength(1000)]
    public string Descricao { get; set; } = string.Empty;

    [Display(Name = "Endereço IP")]
    [StringLength(60)]
    public string? EnderecoIp { get; set; }
}
