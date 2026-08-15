using System.Threading.Channels;
using Elcop.TI.Application.Common;

namespace Elcop.TI.Infrastructure.BackgroundJobs;

/// <summary>Fila de tarefas background usando System.Threading.Channels (thread-safe, lock-free).</summary>
public sealed class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<Func<CancellationToken, ValueTask>> _channel;

    public BackgroundTaskQueue(int capacidade = 100)
    {
        var opcoes = new BoundedChannelOptions(capacidade)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _channel = Channel.CreateBounded<Func<CancellationToken, ValueTask>>(opcoes);
    }

    public async ValueTask EnfileirarAsync(
        Func<CancellationToken, ValueTask> trabalho,
        CancellationToken ct = default)
    {
        if (trabalho is null) throw new ArgumentNullException(nameof(trabalho));

        await _channel.Writer.WriteAsync(trabalho, ct);
    }

    public async ValueTask<Func<CancellationToken, ValueTask>> DesenfileirarAsync(CancellationToken ct)
    {
        if (await _channel.Reader.WaitToReadAsync(ct))
            return await _channel.Reader.ReadAsync(ct);

        throw new OperationCanceledException(ct);
    }
}
