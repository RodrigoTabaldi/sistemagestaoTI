using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Elcop.TI.Web.Infra;

/// <summary>Métricas de baixo custo para coleta por OpenTelemetry/host de produção.</summary>
public static class TelemetriaAplicacao
{
    public const string NomeDoMeter = "Elcop.TI.Web";

    private static readonly Meter Meter = new(NomeDoMeter, "1.0.0");
    private static readonly Counter<long> Requisicoes = Meter.CreateCounter<long>("elcop.http.requests");
    private static readonly Counter<long> Erros = Meter.CreateCounter<long>("elcop.http.errors");
    private static readonly Histogram<double> Duracao = Meter.CreateHistogram<double>("elcop.http.duration", unit: "ms");

    public static void RegistrarRequisicao(string metodo, int status, TimeSpan duracao)
    {
        var tags = new TagList
        {
            { "http.request.method", metodo },
            { "http.response.status_code", status }
        };

        Requisicoes.Add(1, tags);
        Duracao.Record(duracao.TotalMilliseconds, tags);
        if (status >= StatusCodes.Status500InternalServerError)
            Erros.Add(1, tags);
    }
}
