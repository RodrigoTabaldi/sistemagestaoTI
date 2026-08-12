using System.ComponentModel.DataAnnotations;

namespace Elcop.TI.Domain.Enums;

/// <summary>
/// Agrupamento temático das demandas de TI.
/// </summary>
public enum CategoriaDemanda
{
    [Display(Name = "Suporte ao usuário")]
    Suporte = 1,

    [Display(Name = "Hardware")]
    Hardware = 2,

    [Display(Name = "Software")]
    Software = 3,

    [Display(Name = "Rede e conectividade")]
    Rede = 4,

    [Display(Name = "Infraestrutura")]
    Infraestrutura = 5,

    [Display(Name = "Acessos e permissões")]
    Acessos = 6,

    [Display(Name = "Compras e cotações")]
    Compras = 7,

    [Display(Name = "Manutenção preventiva")]
    Manutencao = 8,

    [Display(Name = "Projeto")]
    Projeto = 9,

    [Display(Name = "Segurança da informação")]
    Seguranca = 10,

    [Display(Name = "Outro")]
    Outro = 99
}

/// <summary>
/// Grau de urgência da demanda; direciona o SLA sugerido.
/// </summary>
public enum PrioridadeDemanda
{
    [Display(Name = "Baixa")]
    Baixa = 1,

    [Display(Name = "Média")]
    Media = 2,

    [Display(Name = "Alta")]
    Alta = 3,

    [Display(Name = "Crítica")]
    Critica = 4
}

/// <summary>
/// Colunas do fluxo de trabalho (kanban) das demandas.
/// </summary>
public enum StatusDemanda
{
    [Display(Name = "Aberta")]
    Aberta = 1,

    [Display(Name = "Em andamento")]
    EmAndamento = 2,

    [Display(Name = "Aguardando terceiros")]
    AguardandoTerceiros = 3,

    [Display(Name = "Pausada")]
    Pausada = 4,

    [Display(Name = "Concluída")]
    Concluida = 5,

    [Display(Name = "Cancelada")]
    Cancelada = 6
}

/// <summary>
/// Operação registrada na trilha de auditoria.
/// </summary>
public enum TipoAcaoAuditoria
{
    [Display(Name = "Criação")]
    Criacao = 1,

    [Display(Name = "Alteração")]
    Alteracao = 2,

    [Display(Name = "Exclusão")]
    Exclusao = 3,

    [Display(Name = "Movimentação")]
    Movimentacao = 4,

    [Display(Name = "Login")]
    Login = 5,

    [Display(Name = "Logout")]
    Logout = 6
}
