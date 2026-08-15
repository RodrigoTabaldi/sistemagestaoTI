using Elcop.TI.Application.Common;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Elcop.TI.Infrastructure.BackgroundJobs;

/// <summary>
/// Consumidor de fila: processa tarefas enfileiradas em background.
/// Roda durante toda a vida da aplicação; usa graceful cancellation para desistir de novas
/// tarefas na parada (mas tenta completar as que estão em andamento).
/// </summary>
public class QueuedHostedService : BackgroundService
{
    private readonly IBackgroundTaskQueue _fila;
    private readonly ILogger<QueuedHostedService> _logger;

    public QueuedHostedService(IBackgroundTaskQueue fila, ILogger<QueuedHostedService> logger)
    {
        _fila = fila;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Fila de background iniciada.");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var trabalho = await _fila.DesenfileirarAsync(stoppingToken);

                    try
                    {
                        await trabalho(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao executar tarefa background.");
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
        finally
        {
            _logger.LogInformation("Fila de background parada.");
        }
    }
}
