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

        // Volta para a tela de origem quando possível; caso contrário, para a listagem.
        var retorno = context.HttpContext.Request.Headers.Referer.ToString();

        context.Result = !string.IsNullOrWhiteSpace(retorno) && Uri.IsWellFormedUriString(retorno, UriKind.Absolute)
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
}
