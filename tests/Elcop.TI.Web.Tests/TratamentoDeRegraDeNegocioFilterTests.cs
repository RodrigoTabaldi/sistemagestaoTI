using Elcop.TI.Domain.Common;
using Elcop.TI.Web.Infra;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elcop.TI.Web.Tests;

public sealed class TratamentoDeRegraDeNegocioFilterTests
{
    [Fact]
    public void OnException_ComRefererExterno_NaoRedirecionaParaOutroDominio()
    {
        var filtro = CriarFiltro();
        var contexto = CriarContexto("https://evil.example/phishing");

        filtro.OnException(contexto);

        var resultado = Assert.IsType<RedirectToActionResult>(contexto.Result);
        Assert.Equal("Index", resultado.ActionName);
        Assert.Equal("Ativos", resultado.ControllerName);
        Assert.True(contexto.ExceptionHandled);
    }

    [Fact]
    public void OnException_ComRefererMesmaOrigemMasCaminhoProtocolRelative_NaoRedirecionaParaOutroDominio()
    {
        var filtro = CriarFiltro();
        var contexto = CriarContexto("https://sistema.elcop.com.br//evil.example/phishing");

        filtro.OnException(contexto);

        var resultado = Assert.IsType<RedirectToActionResult>(contexto.Result);
        Assert.Equal("Index", resultado.ActionName);
        Assert.Equal("Ativos", resultado.ControllerName);
        Assert.True(contexto.ExceptionHandled);
    }

    [Fact]
    public void OnException_ComRefererDaMesmaOrigem_RedirecionaParaCaminhoLocal()
    {
        var filtro = CriarFiltro();
        var contexto = CriarContexto("https://sistema.elcop.com.br/Ativos/Criar?aba=dados#foto");

        filtro.OnException(contexto);

        var resultado = Assert.IsType<RedirectResult>(contexto.Result);
        Assert.Equal("/Ativos/Criar?aba=dados#foto", resultado.Url);
        Assert.True(contexto.ExceptionHandled);
    }

    [Theory]
    [InlineData("/Ativos/Criar")]
    [InlineData("/Demandas/Detalhes/1?tab=andamentos")]
    public void OnException_ComRefererLocal_RedirecionaParaCaminhoInformado(string referer)
    {
        var filtro = CriarFiltro();
        var contexto = CriarContexto(referer);

        filtro.OnException(contexto);

        var resultado = Assert.IsType<RedirectResult>(contexto.Result);
        Assert.Equal(referer, resultado.Url);
        Assert.True(contexto.ExceptionHandled);
    }

    private static TratamentoDeRegraDeNegocioFilter CriarFiltro() =>
        new(new TestTempDataDictionaryFactory(), NullLogger<TratamentoDeRegraDeNegocioFilter>.Instance);

    private static ExceptionContext CriarContexto(string referer)
    {
        var http = new DefaultHttpContext();
        http.Request.Scheme = "https";
        http.Request.Host = new HostString("sistema.elcop.com.br");
        http.Request.Headers.Referer = referer;

        var rota = new RouteData();
        rota.Values["controller"] = "Ativos";

        return new ExceptionContext(
            new ActionContext(http, rota, new ActionDescriptor()),
            [])
        {
            Exception = new RegraDeNegocioException("Operacao bloqueada.")
        };
    }

    private sealed class TestTempDataDictionaryFactory : ITempDataDictionaryFactory
    {
        public ITempDataDictionary GetTempData(HttpContext context) =>
            new TempDataDictionary(context, new TestTempDataProvider());
    }

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object?> LoadTempData(HttpContext context) =>
            new Dictionary<string, object?>();

        public void SaveTempData(HttpContext context, IDictionary<string, object?> values)
        {
        }
    }
}
