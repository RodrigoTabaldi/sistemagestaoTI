using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Elcop.TI.Infrastructure.Identity;

/// <summary>
/// Usuário do sistema (quem opera o inventário), distinto do <c>Colaborador</c>,
/// que é a pessoa que recebe os ativos.
/// </summary>
public class ApplicationUser : IdentityUser
{
    [Required]
    [Display(Name = "Nome completo")]
    [StringLength(160)]
    public string NomeCompleto { get; set; } = string.Empty;

    [Display(Name = "Cargo")]
    [StringLength(120)]
    public string? Cargo { get; set; }

    [Display(Name = "Usuário habilitado")]
    public bool Habilitado { get; set; } = true;

    [Display(Name = "Criado em")]
    public DateTime CriadoEm { get; set; } = DateTime.Now;

    [Display(Name = "Último acesso")]
    public DateTime? UltimoAcesso { get; set; }

    [Display(Name = "Tema preferido")]
    [StringLength(10)]
    public string? Tema { get; set; }

    public string Iniciais
    {
        get
        {
            var partes = NomeCompleto.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length == 0) return UserName?[..1].ToUpperInvariant() ?? "?";
            if (partes.Length == 1) return partes[0][..1].ToUpperInvariant();
            return string.Concat(partes[0][..1], partes[^1][..1]).ToUpperInvariant();
        }
    }
}

/// <summary>Perfis de acesso reconhecidos pelo sistema.</summary>
public static class Perfis
{
    public const string Administrador = "Administrador";
    public const string Tecnico = "Tecnico";
    public const string Consulta = "Consulta";

    /// <summary>Pode cadastrar, movimentar e tratar demandas.</summary>
    public const string Operacao = $"{Administrador},{Tecnico}";

    public static readonly IReadOnlyDictionary<string, string> Descricoes = new Dictionary<string, string>
    {
        [Administrador] = "Acesso total, incluindo usuários, cadastros de apoio e auditoria.",
        [Tecnico] = "Cadastra ativos, registra entregas/devoluções e trata demandas.",
        [Consulta] = "Somente leitura de inventário, movimentações e demandas."
    };

    public static readonly IReadOnlyList<string> Todos = new[] { Administrador, Tecnico, Consulta };
}
