using Elcop.TI.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elcop.TI.Application;

/// <summary>
/// Registro dos serviços de aplicação. Todos têm tempo de vida <c>Scoped</c>
/// por compartilharem o mesmo <c>DbContext</c> da requisição.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AdicionarCamadaDeAplicacao(this IServiceCollection services)
    {
        services.AddScoped<IAuditoriaService, AuditoriaService>();
        services.AddScoped<IAtivoService, AtivoService>();
        services.AddScoped<IColaboradorService, ColaboradorService>();
        services.AddScoped<IMovimentacaoService, MovimentacaoService>();
        services.AddScoped<IDemandaService, DemandaService>();
        services.AddScoped<ICadastroService, CadastroService>();
        services.AddScoped<IPainelService, PainelService>();
        services.AddScoped<IRelatorioService, RelatorioService>();

        return services;
    }
}
