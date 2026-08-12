using Elcop.TI.Application.Models;

namespace Elcop.TI.Application.Services;

public interface IPainelService
{
    /// <summary>Monta todos os indicadores, séries e alertas da tela inicial.</summary>
    Task<PainelModel> ObterAsync(CancellationToken ct = default);
}
