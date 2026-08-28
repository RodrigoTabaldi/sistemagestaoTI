using Elcop.TI.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Elcop.TI.Web.Infra;

/// <summary>
/// Converte <see cref="RegraDeNegocioException"/> em mensagem amigável na tela,
/// evitando página de erro para situações previstas (ex.: patrimônio duplicado).
/// </summary>
public class TratamentoDeRegraDeNegocioFilter : IExceptionFilter
{
    private readonly ITempDataDictionaryFactory _tempDataFactory;
    private readonly ILogger<TratamentoDeRegraDeNegocioFilter> _logger;

    public TratamentoDeRegraDeNegocioFilter(
        ITempDataDictionaryFactory tempDataFactory,
        ILogger<TratamentoDeRegraDeNegocioFilter> logger)
    {
        _tempDataFactory = tempDataFactory;
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not RegraDeNegocioException excecao) return;

        _logger.LogInformation("Regra de negócio bloqueou a operação: {Mensagem}", excecao.Message);

        if (EhRequisicaoAjax(context.HttpContext.Request))
        {
            context.Result = new JsonResult(new { sucesso = false, mensagem = excecao.Message })
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
            context.ExceptionHandled = true;
            return;
        }

        var tempData = _tempDataFactory.GetTempData(context.HttpContext);
        tempData[Notificacao.ChaveErro] = excecao.Message;

        // Volta para a tela de origem quando possível, mas o Referer vem do cliente:
        // aceite apenas caminhos locais ou URLs absolutas da mesma origem.
        var retorno = ObterRetornoSeguro(context.HttpContext.Request);

        context.Result = !string.IsNullOrWhiteSpace(retorno)
            ? new RedirectResult(retorno)
            : new RedirectToActionResult(
                "Index",
                context.RouteData.Values["controller"]?.ToString() ?? "Painel",
                null);

        context.ExceptionHandled = true;
    }

    private static bool EhRequisicaoAjax(HttpRequest request) =>
        request.Headers.XRequestedWith == "XMLHttpRequest"
        || request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase);

    private static string? ObterRetornoSeguro(HttpRequest request)
    {
        var retorno = request.Headers.Referer.ToString();
        if (string.IsNullOrWhiteSpace(retorno)) return null;

        if (EhUrlLocal(retorno)) return retorno;

        if (!Uri.TryCreate(retorno, UriKind.Absolute, out var uri)) return null;

        var mesmaOrigem =
            string.Equals(uri.Scheme, request.Scheme, StringComparison.OrdinalIgnoreCase)
            && string.Equals(uri.Authority, request.Host.Value, StringComparison.OrdinalIgnoreCase);

        var caminhoLocal = uri.PathAndQuery + uri.Fragment;
        return mesmaOrigem && EhUrlLocal(caminhoLocal) ? caminhoLocal : null;
    }

    private static bool EhUrlLocal(string url) =>
        url.Length > 0
        && url[0] == '/'
        && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'))
        && !url.StartsWith("/\\", StringComparison.Ordinal);
}
