using Elcop.TI.Application.Common;
using Microsoft.Extensions.Caching.Memory;

namespace Elcop.TI.Infrastructure.Caching;

/// <summary>Implementação de cache em memória do processo (via IMemoryCache).</summary>
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;

    public MemoryCacheService(IMemoryCache cache) => _cache = cache;

    public async Task<T> ObterOuCriarAsync<T>(
        string chave,
        TimeSpan duracao,
        Func<Task<T>> factory,
        CancellationToken ct = default)
    {
        if (_cache.TryGetValue(chave, out T? valor) && valor is not null)
            return valor;

        var resultado = await factory();

        _cache.Set(chave, resultado, duracao);
        return resultado;
    }

    public void Remover(string chave) => _cache.Remove(chave);
}
