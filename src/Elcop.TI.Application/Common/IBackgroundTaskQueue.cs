namespace Elcop.TI.Application.Common;

/// <summary>
/// Fila em memória para tarefas assíncronas executadas fora da requisição HTTP.
/// Padrão: fire-and-forget (enfileirador não espera pelo resultado).
/// Implementação usa System.Threading.Channels.
/// </summary>
public interface IBackgroundTaskQueue
{
    /// <summary>Enfileira uma tarefa assíncrona.</summary>
    /// <param name="trabalho">Função que executa a tarefa (recebe CancellationToken para graceful shutdown).</param>
    ValueTask EnfileirarAsync(
        Func<CancellationToken, ValueTask> trabalho,
        CancellationToken ct = default);

    /// <summary>Desenfileira a próxima tarefa (usada pelo hosted service consumer).</summary>
    ValueTask<Func<CancellationToken, ValueTask>> DesenfileirarAsync(CancellationToken ct);
}
