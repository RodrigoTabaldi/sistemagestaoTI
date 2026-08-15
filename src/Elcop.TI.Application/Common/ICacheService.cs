namespace Elcop.TI.Application.Common;

/// <summary>
/// Abstração de cache em memória. Permitapenas leitura frequente com invalidação explícita.
/// Implementação futura pode usar IDistributedCache (Redis) sem mudar os consumidores.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Obtém valor do cache ou executa a factory para populá-lo.
    /// </summary>
    Task<T> ObterOuCriarAsync<T>(
        string chave,
        TimeSpan duracao,
        Func<Task<T>> factory,
        CancellationToken ct = default);

    /// <summary>Remove a chave do cache (invalidação manual).</summary>
    void Remover(string chave);
}
