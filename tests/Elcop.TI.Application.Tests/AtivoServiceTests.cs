using Elcop.TI.Application.Models;
using Elcop.TI.Application.Services;
using Elcop.TI.Domain.Common;
using Elcop.TI.Domain.Entities;
using Elcop.TI.Domain.Enums;
using Elcop.TI.Infrastructure.Persistence;

namespace Elcop.TI.Application.Tests;

public sealed class AtivoServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private static Ativo NovoAtivo(string patrimonio = "TI-000001", string? numeroSerie = null) => new()
    {
        Patrimonio = patrimonio,
        Tipo = TipoAtivo.Notebook,
        Marca = "Dell",
        Modelo = "Latitude 5420",
        NumeroSerie = numeroSerie,
        Status = StatusAtivo.Disponivel,
        Condicao = CondicaoAtivo.Novo
    };

    private AtivoService CriarServico(AppDbContext db) =>
        new(db, new FakeUsuarioAtual(), new AuditoriaService(db, new FakeUsuarioAtual()));

    [Fact]
    public async Task CriarAsync_ComPatrimonioDuplicado_LancaRegraDeNegocio()
    {
        await using var db = _factory.Criar();
        var servico = CriarServico(db);

        await servico.CriarAsync(NovoAtivo("TI-000001"));

        var duplicado = NovoAtivo("ti-000001");
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => servico.CriarAsync(duplicado));
    }

    [Fact]
    public async Task CriarAsync_ComNumeroSerieDuplicado_LancaRegraDeNegocio()
    {
        await using var db = _factory.Criar();
        var servico = CriarServico(db);

        await servico.CriarAsync(NovoAtivo("TI-000001", "SN-ABC"));

        var duplicado = NovoAtivo("TI-000002", "sn-abc");
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => servico.CriarAsync(duplicado));
    }

    [Fact]
    public async Task AtualizarAsync_IgnoraCamposGovernadosPeloSistemaEnviadosPeloFormulario()
    {
        await using var db = _factory.Criar();
        var servico = CriarServico(db);

        var id = await servico.CriarAsync(NovoAtivo());
        var original = await servico.ObterAsync(id);
        Assert.NotNull(original);
        var criadoEmOriginal = original!.CriadoEm;
        var criadoPorOriginal = original.CriadoPor;

        // Simula um POST malicioso tentando se autoatribuir a posse do ativo e apagar
        // a trilha de criação — nenhum desses campos deveria ser aceito do cliente.
        var manipulado = NovoAtivo();
        manipulado.Id = id;
        manipulado.ColaboradorAtualId = 999;
        manipulado.Excluido = true;
        manipulado.CriadoEm = DateTime.Now.AddYears(-5);
        manipulado.CriadoPor = "invasor";
        manipulado.Marca = "Marca Legítima Atualizada";

        await servico.AtualizarAsync(manipulado);

        var atualizado = await servico.ObterAsync(id);
        Assert.NotNull(atualizado);
        Assert.Null(atualizado!.ColaboradorAtualId);
        Assert.False(atualizado.Excluido);
        Assert.Equal(criadoEmOriginal, atualizado.CriadoEm);
        Assert.Equal(criadoPorOriginal, atualizado.CriadoPor);
        // O campo legítimo enviado no mesmo POST deve ter sido aplicado normalmente.
        Assert.Equal("Marca Legítima Atualizada", atualizado.Marca);
    }

    [Fact]
    public async Task ExcluirAsync_ComColaboradorAssociado_LancaRegraDeNegocio()
    {
        await using var db = _factory.Criar();
        var servico = CriarServico(db);

        var colaborador = new Colaborador
        {
            NomeCompleto = "Fulano de Tal",
            Matricula = "1234",
            Email = "fulano@elcop.com.br"
        };
        db.Colaboradores.Add(colaborador);
        await db.SaveChangesAsync();

        var id = await servico.CriarAsync(NovoAtivo());
        var ativo = await servico.ObterAsync(id);
        ativo!.ColaboradorAtualId = colaborador.Id;
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<RegraDeNegocioException>(() => servico.ExcluirAsync(id));
    }

    [Fact]
    public async Task ExcluirAsync_ExclusaoLogica_SomeDaListaMasPatrimonioContinuaReservado()
    {
        await using var db = _factory.Criar();
        var servico = CriarServico(db);

        var id = await servico.CriarAsync(NovoAtivo("TI-000042"));
        await servico.ExcluirAsync(id);

        Assert.Null(await servico.ObterAsync(id));

        // O número de patrimônio não pode ser reciclado por um ativo novo mesmo depois
        // que o antigo foi excluído logicamente.
        Assert.True(await servico.PatrimonioEmUsoAsync("TI-000042"));
    }

    [Fact]
    public async Task ListarAsync_FiltraPorBuscaNoPatrimonio()
    {
        await using var db = _factory.Criar();
        var servico = CriarServico(db);

        await servico.CriarAsync(NovoAtivo("TI-000001"));
        await servico.CriarAsync(NovoAtivo("TI-000002"));

        var resultado = await servico.ListarAsync(new AtivoFiltro { Busca = "TI-000002" });

        Assert.Single(resultado.Itens);
        Assert.Equal("TI-000002", resultado.Itens[0].Patrimonio);
    }
}
