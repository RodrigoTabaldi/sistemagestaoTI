using Elcop.TI.Application.Models;
using Elcop.TI.Application.Services;
using Elcop.TI.Domain.Common;
using Elcop.TI.Domain.Entities;
using Elcop.TI.Domain.Enums;
using Elcop.TI.Infrastructure.Persistence;

namespace Elcop.TI.Application.Tests;

public sealed class DemandaServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private static Demanda NovaDemanda() => new()
    {
        Titulo = "Impressora não liga",
        Descricao = "Impressora do 3º andar não sai do modo de espera.",
        Categoria = CategoriaDemanda.Suporte,
        Prioridade = PrioridadeDemanda.Media,
        Status = StatusDemanda.Aberta
    };

    private DemandaService CriarServico(AppDbContext db) =>
        new(db, new FakeUsuarioAtual(), new AuditoriaService(db, new FakeUsuarioAtual()));

    [Fact]
    public async Task CriarAsync_GeraCodigoSequencialNoAnoAtual()
    {
        await using var db = _factory.Criar();
        var servico = CriarServico(db);

        var id1 = await servico.CriarAsync(NovaDemanda());
        var id2 = await servico.CriarAsync(NovaDemanda());

        var d1 = await servico.ObterAsync(id1);
        var d2 = await servico.ObterAsync(id2);

        var prefixo = $"DEM-{DateTime.Now.Year}-";
        Assert.StartsWith(prefixo, d1!.Codigo);
        Assert.StartsWith(prefixo, d2!.Codigo);
        Assert.NotEqual(d1.Codigo, d2.Codigo);
    }

    [Fact]
    public async Task CriarAsync_RegistraAndamentoInicialAutomatico()
    {
        await using var db = _factory.Criar();
        var servico = CriarServico(db);

        var id = await servico.CriarAsync(NovaDemanda());
        var demanda = await servico.ObterCompletaAsync(id);

        Assert.NotNull(demanda);
        Assert.Single(demanda!.Andamentos);
        Assert.True(demanda.Andamentos.First().Automatico);
    }

    [Fact]
    public async Task CriarAsync_SemPrazoDefinido_AplicaSlaDaPrioridade()
    {
        await using var db = _factory.Criar();
        var servico = CriarServico(db);

        var critica = NovaDemanda();
        critica.Prioridade = PrioridadeDemanda.Critica;

        var id = await servico.CriarAsync(critica);
        var demanda = await servico.ObterAsync(id);

        Assert.NotNull(demanda!.PrazoLimite);
        // SLA de prioridade crítica é 4h — bem menor que 1 dia.
        Assert.True(demanda.PrazoLimite < DateTime.Now.AddDays(1));
    }

    [Fact]
    public async Task AtualizarAsync_IgnoraCodigoECamposGovernadosPeloSistema()
    {
        await using var db = _factory.Criar();
        var servico = CriarServico(db);

        var id = await servico.CriarAsync(NovaDemanda());
        var original = await servico.ObterAsync(id);
        var codigoOriginal = original!.Codigo;
        var criadoEmOriginal = original.CriadoEm;

        var manipulada = NovaDemanda();
        manipulada.Id = id;
        manipulada.Codigo = "DEM-2020-9999";
        manipulada.CriadoEm = DateTime.Now.AddYears(-1);
        manipulada.Excluido = true;
        manipulada.Titulo = "Título atualizado legitimamente";

        await servico.AtualizarAsync(manipulada);

        var atualizada = await servico.ObterAsync(id);
        Assert.Equal(codigoOriginal, atualizada!.Codigo);
        Assert.Equal(criadoEmOriginal, atualizada.CriadoEm);
        Assert.False(atualizada.Excluido);
        Assert.Equal("Título atualizado legitimamente", atualizada.Titulo);
    }

    [Fact]
    public async Task AtualizarAsync_MudandoStatusParaConcluida_RegistraDataDeConclusaoEAndamento()
    {
        await using var db = _factory.Criar();
        var servico = CriarServico(db);

        var id = await servico.CriarAsync(NovaDemanda());

        var conclusao = NovaDemanda();
        conclusao.Id = id;
        conclusao.Status = StatusDemanda.Concluida;

        await servico.AtualizarAsync(conclusao);

        var demanda = await servico.ObterCompletaAsync(id);
        Assert.Equal(StatusDemanda.Concluida, demanda!.Status);
        Assert.NotNull(demanda.DataConclusao);
        Assert.Equal(100, demanda.PercentualConclusao);
        // Andamento de abertura + andamento automático da mudança de status.
        Assert.Equal(2, demanda.Andamentos.Count);
    }

    [Fact]
    public async Task AdicionarAndamentoAsync_AcumulaTempoGastoEAtualizaPercentual()
    {
        await using var db = _factory.Criar();
        var servico = CriarServico(db);

        var id = await servico.CriarAsync(NovaDemanda());

        await servico.AdicionarAndamentoAsync(new NovoAndamentoModel
        {
            DemandaId = id,
            Descricao = "Verificado o cabo de energia.",
            TempoGastoMinutos = 20,
            PercentualConclusao = 40
        });

        await servico.AdicionarAndamentoAsync(new NovoAndamentoModel
        {
            DemandaId = id,
            Descricao = "Trocada a fonte.",
            TempoGastoMinutos = 15,
            PercentualConclusao = 100
        });

        var demanda = await servico.ObterAsync(id);
        Assert.Equal(35, demanda!.TempoGastoMinutos);
        Assert.Equal(100, demanda.PercentualConclusao);
    }

    [Fact]
    public async Task ObterContadoresAsync_ContaAtrasadasCriticasEConcluidasCorretamente()
    {
        await using var db = _factory.Criar();
        var servico = CriarServico(db);

        var atrasada = NovaDemanda();
        atrasada.Prioridade = PrioridadeDemanda.Critica;
        var idAtrasada = await servico.CriarAsync(atrasada);
        var entidadeAtrasada = await servico.ObterAsync(idAtrasada);
        entidadeAtrasada!.PrazoLimite = DateTime.Today.AddDays(-2);
        await db.SaveChangesAsync();

        await servico.CriarAsync(NovaDemanda());

        var contadores = await servico.ObterContadoresAsync();

        Assert.Equal(2, contadores.Abertas);
        Assert.Equal(1, contadores.Atrasadas);
        Assert.Equal(1, contadores.Criticas);
    }
}
