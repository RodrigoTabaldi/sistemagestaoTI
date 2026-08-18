using Elcop.TI.Application.Services;
using Elcop.TI.Domain.Entities;
using Elcop.TI.Domain.Enums;

namespace Elcop.TI.Application.Tests;

public sealed class PainelServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task ObterAsync_ComBaseVazia_NaoLancaERetornaZerados()
    {
        await using var db = _factory.Criar();
        var painel = new PainelService(db);

        var resultado = await painel.ObterAsync();

        Assert.Equal(0, resultado.TotalAtivos);
        Assert.Equal(0, resultado.TotalColaboradores);
        Assert.Empty(resultado.Alertas);
        // Série de 6 meses sempre tem 6 pontos, mesmo sem dados — é isso que evita a
        // linha do gráfico "pular" no front quando um mês não teve nenhum registro.
        Assert.Equal(6, resultado.MovimentacoesPorMes.Count);
        Assert.Equal(6, resultado.DemandasPorMes.Count);
    }

    [Fact]
    public async Task ObterAsync_SomaValorDoInventarioEContaAtivosDisponiveis()
    {
        await using var db = _factory.Criar();
        var usuario = new FakeUsuarioAtual();
        var ativos = new AtivoService(db, usuario, new AuditoriaService(db, usuario));

        await ativos.CriarAsync(new Ativo
        {
            Patrimonio = "TI-000001", Tipo = TipoAtivo.Notebook, Marca = "Dell", Modelo = "Latitude",
            Status = StatusAtivo.Disponivel, ValorAquisicao = 3500m
        });
        await ativos.CriarAsync(new Ativo
        {
            Patrimonio = "TI-000002", Tipo = TipoAtivo.Monitor, Marca = "LG", Modelo = "24\"",
            Status = StatusAtivo.EmManutencao, ValorAquisicao = 900m
        });

        var painel = new PainelService(db);
        var resultado = await painel.ObterAsync();

        Assert.Equal(2, resultado.TotalAtivos);
        Assert.Equal(1, resultado.AtivosDisponiveis);
        Assert.Equal(1, resultado.AtivosEmManutencao);
        Assert.Equal(4400m, resultado.ValorInventario);
    }

    [Fact]
    public async Task ObterAsync_ComEstoqueZerado_GeraAlertaDeEstoqueSemItens()
    {
        await using var db = _factory.Criar();
        var usuario = new FakeUsuarioAtual();
        var ativos = new AtivoService(db, usuario, new AuditoriaService(db, usuario));

        // EmManutencao porque um ativo recém-criado sem colaborador é renormalizado para
        // Disponivel pelo AtivoService quando o status enviado é EmUso (NormalizarPosse).
        await ativos.CriarAsync(new Ativo
        {
            Patrimonio = "TI-000001", Tipo = TipoAtivo.Notebook, Marca = "Dell", Modelo = "Latitude",
            Status = StatusAtivo.EmManutencao
        });

        var painel = new PainelService(db);
        var resultado = await painel.ObterAsync();

        Assert.Contains(resultado.Alertas, a => a.Titulo.Contains("sem itens disponíveis"));
    }
}
