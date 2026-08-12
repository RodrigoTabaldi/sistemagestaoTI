namespace Elcop.TI.Domain.Common;

/// <summary>
/// Raiz comum a todas as entidades persistidas: identidade, trilha de criação/alteração
/// e exclusão lógica (nenhum registro histórico é removido fisicamente do inventário).
/// </summary>
public abstract class EntidadeBase
{
    public int Id { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.Now;

    public string? CriadoPor { get; set; }

    public DateTime? AtualizadoEm { get; set; }

    public string? AtualizadoPor { get; set; }

    public bool Excluido { get; set; }

    public void MarcarCriacao(string? usuario)
    {
        CriadoEm = DateTime.Now;
        CriadoPor = usuario;
    }

    public void MarcarAtualizacao(string? usuario)
    {
        AtualizadoEm = DateTime.Now;
        AtualizadoPor = usuario;
    }
}
