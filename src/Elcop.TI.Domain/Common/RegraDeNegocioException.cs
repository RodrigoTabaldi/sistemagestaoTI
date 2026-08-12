namespace Elcop.TI.Domain.Common;

/// <summary>
/// Violação de regra de negócio prevista (ex.: entregar um ativo que já está em posse).
/// A camada Web converte em mensagem de erro para o usuário, sem stack trace.
/// </summary>
public class RegraDeNegocioException : Exception
{
    public RegraDeNegocioException(string mensagem) : base(mensagem) { }

    public RegraDeNegocioException(string mensagem, Exception inner) : base(mensagem, inner) { }
}
