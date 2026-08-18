using Elcop.TI.Application.Models;
using Elcop.TI.Application.Services;
using Elcop.TI.Domain.Common;
using Elcop.TI.Domain.Entities;
using Elcop.TI.Domain.Enums;
using Elcop.TI.Infrastructure.Persistence;

namespace Elcop.TI.Application.Tests;

public sealed class MovimentacaoServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private static Ativo NovoAtivo(string patrimonio = "TI-000001") => new()
    {
        Patrimonio = patrimonio,
        Tipo = TipoAtivo.Notebook,
        Marca = "Dell",
        Modelo = "Latitude 5420",
        Status = StatusAtivo.Disponivel
    };

    private static Colaborador NovoColaborador(string matricula, string email) => new()
    {
        NomeCompleto = "Pessoa " + matricula,
        Matricula = matricula,
        Email = email,
        Status = StatusColaborador.Ativo
    };

    private static EntregaAtivoModel NovaEntrega(int ativoId, int colaboradorId) => new()
    {
        AtivoId = ativoId,
        ColaboradorId = colaboradorId,
        Tipo = TipoMovimentacao.Entrega,
        DataRetirada = DateTime.Now,
        CondicaoRetirada = CondicaoAtivo.Bom,
        HerdarLotacao = false
    };

    private (AtivoService ativos, ColaboradorService colaboradores, MovimentacaoService movimentacoes) CriarServicos(AppDbContext db)
    {
        var usuario = new FakeUsuarioAtual();
        var auditoria = new AuditoriaService(db, usuario);
        return (
            new AtivoService(db, usuario, auditoria),
            new ColaboradorService(db, usuario, auditoria),
            new MovimentacaoService(db, usuario, auditoria));
    }

    [Fact]
    public async Task RegistrarEntregaAsync_MarcaAtivoComoEmUsoEAtribuiPosse()
    {
        await using var db = _factory.Criar();
        var (ativos, colaboradores, movimentacoes) = CriarServicos(db);

        var ativoId = await ativos.CriarAsync(NovoAtivo());
        var colaboradorId = await colaboradores.CriarAsync(NovoColaborador("0001", "a@elcop.com.br"));

        await movimentacoes.RegistrarEntregaAsync(NovaEntrega(ativoId, colaboradorId));

        var ativo = await ativos.ObterAsync(ativoId);
        Assert.Equal(colaboradorId, ativo!.ColaboradorAtualId);
        Assert.Equal(StatusAtivo.EmUso, ativo.Status);
    }

    [Fact]
    public async Task RegistrarEntregaAsync_AtivoJaEmPosse_LancaRegraDeNegocio()
    {
        await using var db = _factory.Criar();
        var (ativos, colaboradores, movimentacoes) = CriarServicos(db);

        var ativoId = await ativos.CriarAsync(NovoAtivo());
        var colaborador1 = await colaboradores.CriarAsync(NovoColaborador("0001", "a@elcop.com.br"));
        var colaborador2 = await colaboradores.CriarAsync(NovoColaborador("0002", "b@elcop.com.br"));

        await movimentacoes.RegistrarEntregaAsync(NovaEntrega(ativoId, colaborador1));

        await Assert.ThrowsAsync<RegraDeNegocioException>(
            () => movimentacoes.RegistrarEntregaAsync(NovaEntrega(ativoId, colaborador2)));
    }

    [Fact]
    public async Task RegistrarEntregaAsync_ColaboradorDesligado_LancaRegraDeNegocio()
    {
        await using var db = _factory.Criar();
        var (ativos, colaboradores, movimentacoes) = CriarServicos(db);

        var ativoId = await ativos.CriarAsync(NovoAtivo());
        var colaboradorId = await colaboradores.CriarAsync(NovoColaborador("0001", "a@elcop.com.br"));

        var colaborador = await colaboradores.ObterAsync(colaboradorId);
        colaborador!.Status = StatusColaborador.Desligado;
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<RegraDeNegocioException>(
            () => movimentacoes.RegistrarEntregaAsync(NovaEntrega(ativoId, colaboradorId)));
    }

    [Fact]
    public async Task RegistrarDevolucaoAsync_LiberaOAtivoParaEstoque()
    {
        await using var db = _factory.Criar();
        var (ativos, colaboradores, movimentacoes) = CriarServicos(db);

        var ativoId = await ativos.CriarAsync(NovoAtivo());
        var colaboradorId = await colaboradores.CriarAsync(NovoColaborador("0001", "a@elcop.com.br"));
        var movimentacaoId = await movimentacoes.RegistrarEntregaAsync(NovaEntrega(ativoId, colaboradorId));

        await movimentacoes.RegistrarDevolucaoAsync(new DevolucaoAtivoModel
        {
            MovimentacaoId = movimentacaoId,
            DataDevolucao = DateTime.Now,
            CondicaoDevolucao = CondicaoAtivo.Bom,
            StatusDestino = StatusAtivo.Disponivel,
            DataRetiradaOriginal = DateTime.Now.AddDays(-1)
        });

        var ativo = await ativos.ObterAsync(ativoId);
        Assert.Null(ativo!.ColaboradorAtualId);
        Assert.Equal(StatusAtivo.Disponivel, ativo.Status);

        var movimentacao = await movimentacoes.ObterAsync(movimentacaoId);
        Assert.Equal(StatusMovimentacao.Devolvido, movimentacao!.Status);
    }

    [Fact]
    public async Task RegistrarDevolucaoAsync_JaDevolvida_LancaRegraDeNegocio()
    {
        await using var db = _factory.Criar();
        var (ativos, colaboradores, movimentacoes) = CriarServicos(db);

        var ativoId = await ativos.CriarAsync(NovoAtivo());
        var colaboradorId = await colaboradores.CriarAsync(NovoColaborador("0001", "a@elcop.com.br"));
        var movimentacaoId = await movimentacoes.RegistrarEntregaAsync(NovaEntrega(ativoId, colaboradorId));

        var devolucao = new DevolucaoAtivoModel
        {
            MovimentacaoId = movimentacaoId,
            DataDevolucao = DateTime.Now,
            StatusDestino = StatusAtivo.Disponivel,
            DataRetiradaOriginal = DateTime.Now.AddDays(-1)
        };

        await movimentacoes.RegistrarDevolucaoAsync(devolucao);

        await Assert.ThrowsAsync<RegraDeNegocioException>(
            () => movimentacoes.RegistrarDevolucaoAsync(devolucao));
    }

    [Fact]
    public async Task TransferirAsync_MovePosseParaNovoColaboradorAtomicamente()
    {
        await using var db = _factory.Criar();
        var (ativos, colaboradores, movimentacoes) = CriarServicos(db);

        var ativoId = await ativos.CriarAsync(NovoAtivo());
        var colaborador1 = await colaboradores.CriarAsync(NovoColaborador("0001", "a@elcop.com.br"));
        var colaborador2 = await colaboradores.CriarAsync(NovoColaborador("0002", "b@elcop.com.br"));

        var movimentacaoOrigemId = await movimentacoes.RegistrarEntregaAsync(NovaEntrega(ativoId, colaborador1));

        var novaMovimentacaoId = await movimentacoes.TransferirAsync(movimentacaoOrigemId, colaborador2, "Troca de setor");

        var ativo = await ativos.ObterAsync(ativoId);
        Assert.Equal(colaborador2, ativo!.ColaboradorAtualId);

        var origem = await movimentacoes.ObterAsync(movimentacaoOrigemId);
        Assert.Equal(StatusMovimentacao.Devolvido, origem!.Status);

        var nova = await movimentacoes.ObterAsync(novaMovimentacaoId);
        Assert.Equal(colaborador2, nova!.ColaboradorId);
        Assert.True(nova.EstaEmAberto);
    }

    [Fact]
    public async Task CancelarAsync_DevolveAtivoAoEstoqueSeAindaEmPosseDoMesmoColaborador()
    {
        await using var db = _factory.Criar();
        var (ativos, colaboradores, movimentacoes) = CriarServicos(db);

        var ativoId = await ativos.CriarAsync(NovoAtivo());
        var colaboradorId = await colaboradores.CriarAsync(NovoColaborador("0001", "a@elcop.com.br"));
        var movimentacaoId = await movimentacoes.RegistrarEntregaAsync(NovaEntrega(ativoId, colaboradorId));

        await movimentacoes.CancelarAsync(movimentacaoId, "Registrado por engano");

        var ativo = await ativos.ObterAsync(ativoId);
        Assert.Null(ativo!.ColaboradorAtualId);
        Assert.Equal(StatusAtivo.Disponivel, ativo.Status);

        var movimentacao = await movimentacoes.ObterAsync(movimentacaoId);
        Assert.Equal(StatusMovimentacao.Cancelado, movimentacao!.Status);
    }
}
