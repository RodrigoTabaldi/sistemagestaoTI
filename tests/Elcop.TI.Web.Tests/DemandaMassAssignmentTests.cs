using System.Net;
using Elcop.TI.Domain.Entities;
using Elcop.TI.Domain.Enums;
using Elcop.TI.Infrastructure.Identity;
using Elcop.TI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elcop.TI.Web.Tests;

/// <summary>
/// Prova, com uma requisição HTTP de verdade (sem atalho de teste unitário chamando o
/// serviço diretamente), que <c>DemandasController.AplicarLimitesDoSolicitanteAsync</c>
/// realmente descarta campos que um perfil <see cref="Perfis.Consulta"/> não deveria poder
/// definir — mesmo quando o POST os inclui manualmente, como um atacante faria burlando o
/// formulário renderizado (que nem exibe esses campos para este perfil).
/// </summary>
public sealed class DemandaMassAssignmentTests : IAsyncLifetime
{
    private ElcopWebApplicationFactory _factory = null!;

    public Task InitializeAsync()
    {
        _factory = new ElcopWebApplicationFactory();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task Criar_ComPostForjadoPorSolicitante_IgnoraStatusResponsavelPrazoESolicitante()
    {
        // AllowAutoRedirect=false: quero inspecionar a resposta do próprio POST (302 em caso
        // de sucesso), não a página para onde ele redireciona.
        var client = _factory.CriarClienteComo(
            Perfis.Consulta, "solicitante.forjado@elcop.com.br", seguirRedirecionamentos: false);

        var token = await client.ObterTokenAntiFalsificacaoAsync("/Demandas/Criar");

        var titulo = $"Teste mass-assignment {Guid.NewGuid():N}";
        var corpo = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Demanda.Titulo"] = titulo,
            ["Demanda.Categoria"] = nameof(CategoriaDemanda.Hardware),
            ["Demanda.Prioridade"] = nameof(PrioridadeDemanda.Media),

            // Nenhum destes campos aparece no formulário que a view renderiza para um
            // solicitante (ver Formulario.cshtml, blocos "@if (podeOperar)") — só chegam
            // aqui porque um POST pode ser forjado manualmente, sem passar pelo HTML.
            ["Demanda.Status"] = nameof(StatusDemanda.Concluida),
            ["Demanda.Responsavel"] = "Atacante Forjado",
            ["Demanda.PrazoLimite"] = "2099-01-01",
            ["Demanda.SolicitanteId"] = "999999",
            ["Demanda.DepartamentoId"] = "999999",
            ["Demanda.PercentualConclusao"] = "100",
        });

        var resposta = await client.PostAsync("/Demandas/Criar", corpo);

        // 302 = RedirectToAction(Detalhes): a demanda foi aceita e criada de verdade,
        // não voltou para o formulário por causa de um ModelState inválido.
        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);

        using var escopo = _factory.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<AppDbContext>();
        var demanda = await db.Demandas.SingleAsync(d => d.Titulo == titulo);

        Assert.Equal(StatusDemanda.Aberta, demanda.Status);
        Assert.Null(demanda.Responsavel);
        Assert.Null(demanda.SolicitanteId);
        Assert.Null(demanda.DepartamentoId);
        Assert.Equal(0, demanda.PercentualConclusao);
        Assert.NotEqual(new DateTime(2099, 1, 1), demanda.PrazoLimite);
    }

    [Fact]
    public async Task Criar_ComPostDeQuemPodeOperar_AceitaResponsavelEStatusInformados()
    {
        // Contraste com o teste acima: para Administrador/Tecnico, esses mesmos campos
        // são legítimos e devem ser aceitos — a defesa é específica de quem só solicita.
        var client = _factory.CriarClienteComo(
            Perfis.Tecnico, "tecnico@elcop.com.br", seguirRedirecionamentos: false);

        var token = await client.ObterTokenAntiFalsificacaoAsync("/Demandas/Criar");

        var titulo = $"Teste operar {Guid.NewGuid():N}";
        var corpo = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Demanda.Titulo"] = titulo,
            ["Demanda.Categoria"] = nameof(CategoriaDemanda.Software),
            ["Demanda.Prioridade"] = nameof(PrioridadeDemanda.Alta),
            ["Demanda.Status"] = nameof(StatusDemanda.EmAndamento),
            ["Demanda.Responsavel"] = "Técnico de Verdade",
        });

        var resposta = await client.PostAsync("/Demandas/Criar", corpo);

        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);

        using var escopo = _factory.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<AppDbContext>();
        var demanda = await db.Demandas.SingleAsync(d => d.Titulo == titulo);

        Assert.Equal(StatusDemanda.EmAndamento, demanda.Status);
        Assert.Equal("Técnico de Verdade", demanda.Responsavel);
    }
}
