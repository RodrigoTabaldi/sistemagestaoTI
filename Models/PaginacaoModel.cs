using Elcop.TI.Application.Common;

namespace Elcop.TI.Web.Models;

/// <summary>
/// Recorte do <see cref="ResultadoPaginado{T}"/> sem o tipo genérico, para que a
/// partial de paginação sirva a qualquer listagem.
/// </summary>
public class PaginacaoModel
{
    public int PaginaAtual { get; init; } = 1;
    public int TotalPaginas { get; init; }
    public int TotalItens { get; init; }
    public int PrimeiroRegistro { get; init; }
    public int UltimoRegistro { get; init; }
    public bool TemAnterior { get; init; }
    public bool TemProxima { get; init; }

    public static PaginacaoModel De<T>(ResultadoPaginado<T> pagina) => new()
    {
        PaginaAtual = pagina.PaginaAtual,
        TotalPaginas = pagina.TotalPaginas,
        TotalItens = pagina.TotalItens,
        PrimeiroRegistro = pagina.PrimeiroRegistro,
        UltimoRegistro = pagina.UltimoRegistro,
        TemAnterior = pagina.TemAnterior,
        TemProxima = pagina.TemProxima
    };
}
