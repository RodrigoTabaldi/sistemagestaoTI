using Elcop.TI.Application.Models;

namespace Elcop.TI.Web.Models;

/// <summary>Uma série do gráfico de linhas (rótulos vêm dos próprios pontos).</summary>
public class SerieLinha
{
    public string Nome { get; init; } = string.Empty;
    public string Cor { get; init; } = "#8B1F26";
    public IReadOnlyList<ItemGrafico> Pontos { get; init; } = Array.Empty<ItemGrafico>();
}

/// <summary>Gráfico de linhas com uma ou mais séries sobrepostas.</summary>
public class GraficoLinhaModel
{
    public IReadOnlyList<SerieLinha> Series { get; init; } = Array.Empty<SerieLinha>();

    public IReadOnlyList<string> Rotulos =>
        Series.FirstOrDefault()?.Pontos.Select(p => p.Rotulo).ToList() ?? new List<string>();

    public decimal Maximo
    {
        get
        {
            var maior = Series.SelectMany(s => s.Pontos).Select(p => p.Valor).DefaultIfEmpty(0).Max();
            return maior <= 0 ? 1 : maior;
        }
    }
}
