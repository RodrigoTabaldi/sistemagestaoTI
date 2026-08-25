using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Elcop.TI.Web.Tests;

internal static class TestClientExtensions
{
    /// <summary>Cliente autenticado via <see cref="TestAuthHandler"/> com o perfil informado.</summary>
    public static HttpClient CriarClienteComo(
        this ElcopWebApplicationFactory factory,
        string perfil,
        string? email = null,
        bool seguirRedirecionamentos = true)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = seguirRedirecionamentos
        });

        client.DefaultRequestHeaders.Add(TestAuthHandler.CabecalhoPerfil, perfil);
        client.DefaultRequestHeaders.Add(TestAuthHandler.CabecalhoEmail, email ?? "teste@elcop.com.br");
        return client;
    }

    /// <summary>Cliente sem nenhum cabeçalho de autenticação — simula um visitante anônimo.</summary>
    public static HttpClient CriarClienteAnonimo(this ElcopWebApplicationFactory factory) =>
        factory.CreateClient();

    /// <summary>
    /// Busca o token antifalsificação (<c>__RequestVerificationToken</c>) renderizado pelo
    /// tag helper de formulário na página informada — necessário para qualquer POST que
    /// passe por <c>[ValidateAntiForgeryToken]</c>, exatamente como o navegador faria.
    /// </summary>
    public static async Task<string> ObterTokenAntiFalsificacaoAsync(this HttpClient client, string url)
    {
        var html = await client.GetStringAsync(url);
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");

        if (!match.Success)
            throw new InvalidOperationException($"Token antifalsificação não encontrado em {url}.");

        return match.Groups[1].Value;
    }
}
