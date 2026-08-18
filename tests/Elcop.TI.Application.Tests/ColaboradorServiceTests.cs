using Elcop.TI.Application.Services;
using Elcop.TI.Domain.Common;
using Elcop.TI.Domain.Entities;
using Elcop.TI.Domain.Enums;
using Elcop.TI.Infrastructure.Persistence;

namespace Elcop.TI.Application.Tests;

public sealed class ColaboradorServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private static Colaborador NovoColaborador(string matricula = "0001", string email = "pessoa@elcop.com.br") => new()
    {
        NomeCompleto = "Pessoa da Silva",
        Matricula = matricula,
        Email = email,
        Status = StatusColaborador.Ativo
    };

    private ColaboradorService CriarServico(AppDbContext db) =>
        new(db, new FakeUsuarioAtual(), new AuditoriaService(db, new FakeUsuarioAtual()));

    [Fact]
    public async Task CriarAsync_ComMatriculaDuplicada_LancaRegraDeNegocio()
    {
        await using var db = _factory.Criar();
        var servico = CriarServico(db);

        await servico.CriarAsync(NovoColaborador("0001", "a@elcop.com.br"));

        var duplicado = NovoColaborador("0001", "b@elcop.com.br");
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => servico.CriarAsync(duplicado));
    }

    [Fact]
    public async Task CriarAsync_ComEmailDuplicado_ComparaSemDiferenciarMaiusculas()
    {
        await using var db = _factory.Criar();
        var servico = CriarServico(db);

        await servico.CriarAsync(NovoColaborador("0001", "Pessoa@Elcop.com.br"));

        var duplicado = NovoColaborador("0002", "pessoa@elcop.com.br");
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => servico.CriarAsync(duplicado));
    }

    [Fact]
    public async Task AtualizarAsync_IgnoraCamposGovernadosPeloSistema()
    {
        await using var db = _factory.Criar();
        var servico = CriarServico(db);

        var id = await servico.CriarAsync(NovoColaborador());
        var original = await servico.ObterAsync(id);
        var criadoEmOriginal = original!.CriadoEm;

        var manipulado = NovoColaborador();
        manipulado.Id = id;
        manipulado.Excluido = true;
        manipulado.CriadoEm = DateTime.Now.AddYears(-3);
        manipulado.Cargo = "Analista Sênior";

        await servico.AtualizarAsync(manipulado);

        var atualizado = await servico.ObterAsync(id);
        Assert.NotNull(atualizado);
        Assert.False(atualizado!.Excluido);
        Assert.Equal(criadoEmOriginal, atualizado.CriadoEm);
        Assert.Equal("Analista Sênior", atualizado.Cargo);
    }

    [Fact]
    public async Task AtualizarAsync_DesligandoComAtivoEmPosse_LancaRegraDeNegocio()
    {
        await using var db = _factory.Criar();
        var colaboradores = CriarServico(db);
        var ativos = new AtivoService(db, new FakeUsuarioAtual(), new AuditoriaService(db, new FakeUsuarioAtual()));

        var colaboradorId = await colaboradores.CriarAsync(NovoColaborador());

        var ativoId = await ativos.CriarAsync(new Ativo
        {
            Patrimonio = "TI-000001",
            Tipo = TipoAtivo.Notebook,
            Marca = "Dell",
            Modelo = "Latitude",
            Status = StatusAtivo.EmUso
        });

        var ativo = await ativos.ObterAsync(ativoId);
        ativo!.ColaboradorAtualId = colaboradorId;
        await db.SaveChangesAsync();

        var desligamento = NovoColaborador();
        desligamento.Id = colaboradorId;
        desligamento.Status = StatusColaborador.Desligado;

        await Assert.ThrowsAsync<RegraDeNegocioException>(() => colaboradores.AtualizarAsync(desligamento));
    }

    [Fact]
    public async Task ExcluirAsync_ComAtivoEmPosse_LancaRegraDeNegocio()
    {
        await using var db = _factory.Criar();
        var colaboradores = CriarServico(db);
        var ativos = new AtivoService(db, new FakeUsuarioAtual(), new AuditoriaService(db, new FakeUsuarioAtual()));

        var colaboradorId = await colaboradores.CriarAsync(NovoColaborador());
        var ativoId = await ativos.CriarAsync(new Ativo
        {
            Patrimonio = "TI-000002",
            Tipo = TipoAtivo.Notebook,
            Marca = "Dell",
            Modelo = "Latitude",
            Status = StatusAtivo.EmUso
        });

        var ativo = await ativos.ObterAsync(ativoId);
        ativo!.ColaboradorAtualId = colaboradorId;
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<RegraDeNegocioException>(() => colaboradores.ExcluirAsync(colaboradorId));
    }
}
