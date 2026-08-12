using System.ComponentModel.DataAnnotations;

namespace Elcop.TI.Domain.Enums;

/// <summary>
/// Natureza do movimento registrado para um ativo.
/// </summary>
public enum TipoMovimentacao
{
    [Display(Name = "Entrega ao colaborador")]
    Entrega = 1,

    [Display(Name = "Empréstimo temporário")]
    Emprestimo = 2,

    [Display(Name = "Transferência entre colaboradores")]
    Transferencia = 3,

    [Display(Name = "Envio para manutenção")]
    Manutencao = 4,

    [Display(Name = "Baixa / descarte")]
    Baixa = 5
}

/// <summary>
/// Estágio do termo de responsabilidade (retirada x devolução).
/// </summary>
public enum StatusMovimentacao
{
    [Display(Name = "Em posse do colaborador")]
    EmAberto = 1,

    [Display(Name = "Devolvido")]
    Devolvido = 2,

    [Display(Name = "Atrasado")]
    Atrasado = 3,

    [Display(Name = "Cancelado")]
    Cancelado = 4
}

/// <summary>
/// Vínculo do colaborador com a empresa.
/// </summary>
public enum StatusColaborador
{
    [Display(Name = "Ativo")]
    Ativo = 1,

    [Display(Name = "Afastado")]
    Afastado = 2,

    [Display(Name = "Férias")]
    Ferias = 3,

    [Display(Name = "Desligado")]
    Desligado = 4
}
