using Microsoft.EntityFrameworkCore;

namespace Elcop.TI.Application.Common;

/// <summary>
/// Página de resultados com os metadados necessários para o componente de paginação.
/// </summary>
public class ResultadoPaginado<T>
{
    public IReadOnlyList<T> Itens { get; init; } = Array.Empty<T>();

    public int PaginaAtual { get; init; } = 1;

    public int TamanhoPagina { get; init; } = 20;

    public int TotalItens { get; init; }

    public int TotalPaginas => TamanhoPagina <= 0
        ? 0
        : (int)Math.Ceiling(TotalItens / (double)TamanhoPagina);

    public bool TemAnterior => PaginaAtual > 1;

    public bool TemProxima => PaginaAtual < TotalPaginas;

    public int PrimeiroRegistro => TotalItens == 0 ? 0 : ((PaginaAtual - 1) * TamanhoPagina) + 1;

    public int UltimoRegistro => Math.Min(PaginaAtual * TamanhoPagina, TotalItens);

    public static ResultadoPaginado<T> Vazio(int tamanhoPagina = 20) =>
        new() { Itens = Array.Empty<T>(), PaginaAtual = 1, TamanhoPagina = tamanhoPagina, TotalItens = 0 };
}

public static class ConsultaPaginadaExtensions
{
    /// <summary>
    /// Materializa a página solicitada emitindo apenas duas consultas (COUNT + SELECT).
    /// </summary>
    public static async Task<ResultadoPaginado<T>> PaginarAsync<T>(
        this IQueryable<T> consulta,
        int pagina,
        int tamanhoPagina,
        CancellationToken cancellationToken = default)
    {
        pagina = pagina < 1 ? 1 : pagina;
        tamanhoPagina = tamanhoPagina is < 1 or > 200 ? 20 : tamanhoPagina;

        var total = await consulta.CountAsync(cancellationToken);

        // Se um filtro esvaziou a última página, retrocede em vez de mostrar uma lista vazia.
        var totalPaginas = (int)Math.Ceiling(total / (double)tamanhoPagina);
        if (totalPaginas > 0 && pagina > totalPaginas) pagina = totalPaginas;

        var itens = await consulta
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync(cancellationToken);

        return new ResultadoPaginado<T>
        {
            Itens = itens,
            PaginaAtual = pagina,
            TamanhoPagina = tamanhoPagina,
            TotalItens = total
        };
    }
}
