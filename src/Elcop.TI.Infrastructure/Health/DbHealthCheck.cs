using Elcop.TI.Application.Common;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Elcop.TI.Infrastructure.Health;

/// <summary>Verifica se o banco de dados está acessível.</summary>
public class DbHealthCheck : IHealthCheck
{
    private readonly IAppDbContext _db;

    public DbHealthCheck(IAppDbContext db) => _db = db;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await _db.Database.CanConnectAsync(cancellationToken))
                return HealthCheckResult.Unhealthy("Banco de dados não está acessível.");

            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Erro ao verificar banco de dados.", ex);
        }
    }
}
