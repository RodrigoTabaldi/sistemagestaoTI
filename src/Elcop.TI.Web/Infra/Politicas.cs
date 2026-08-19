namespace Elcop.TI.Web.Infra;

/// <summary>Nomes das políticas de autorização usadas nos controllers.</summary>
public static class Politicas
{
    /// <summary>Exclusivo do administrador: usuários, cadastros de apoio e auditoria.</summary>
    public const string Administrar = "Administrar";

    /// <summary>Administrador ou técnico: cria, edita, movimenta e trata demandas.</summary>
    public const string Operar = "Operar";

    /// <summary>
    /// Qualquer perfil, inclusive o de consulta: abrir uma demanda para a TI atender.
    /// Quem abre não conduz o atendimento — status, prazo e responsável seguem sendo
    /// da equipe de TI (ver <see cref="Operar"/>).
    /// </summary>
    public const string AbrirDemanda = "AbrirDemanda";
}
