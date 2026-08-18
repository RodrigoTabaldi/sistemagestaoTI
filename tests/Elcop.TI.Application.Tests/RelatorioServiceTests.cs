using System.Text;
using Elcop.TI.Application.Models;
using Elcop.TI.Application.Services;
using Elcop.TI.Domain.Entities;
using Elcop.TI.Domain.Enums;
using Elcop.TI.Infrastructure.Persistence;

namespace Elcop.TI.Application.Tests;

public sealed class RelatorioServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private static string DecodificarCsv(byte[] conteudo) =>
        // Remove o BOM UTF-8 gravado pelo CsvBuilder antes de comparar como texto.
        Encoding.UTF8.GetString(conteudo).TrimStart('﻿');

    private RelatorioService CriarServico(AppDbContext db)
    {
        var usuario = new FakeUsuarioAtual();
        var auditoria = new AuditoriaService(db, usuario);
        var ativos = new AtivoService(db, usuario, auditoria);
        var colaboradores = new ColaboradorService(db, usuario, auditoria);
        var movimentacoes = new MovimentacaoService(db, usuario, auditoria);
        var demandas = new DemandaService(db, usuario, auditoria);
        return new RelatorioService(ativos, colaboradores, movimentacoes, demandas);
    }

    [Fact]
    public async Task ExportarColaboradoresAsync_CampoComecandoComIgual_RecebePrefixoDeEscapeAntiFormula()
    {
        await using var db = _factory.Criar();
        var usuario = new FakeUsuarioAtual();
        var colaboradores = new ColaboradorService(db, usuario, new AuditoriaService(db, usuario));

        // Payload clássico de CSV/formula injection: se aberto sem escape no Excel,
        // esse campo tentaria executar cmd.exe ao abrir a planilha.
        await colaboradores.CriarAsync(new Colaborador
        {
            NomeCompleto = "Fulano de Tal",
            Matricula = "0001",
            Email = "fulano@elcop.com.br",
            Telefone = "=cmd|'/c calc'!A1"
        });

        var relatorios = CriarServico(db);
        var csv = DecodificarCsv(await relatorios.ExportarColaboradoresAsync(new ColaboradorFiltro()));

        // O campo precisa carregar o apóstrofo neutralizador logo antes do '=' — se o Excel
        // abrisse isso sem o prefixo, o valor apareceria como ";=cmd|" (fórmula "solta").
        Assert.DoesNotContain(";=cmd|", csv);
        Assert.Contains("'=cmd|", csv);
    }

    [Theory]
    [InlineData("+1234567890")]
    [InlineData("-1234567890")]
    [InlineData("@SUM(A1:A9)")]
    public async Task ExportarColaboradoresAsync_OutrosGatilhosDeFormula_TambemRecebemPrefixo(string valorPerigoso)
    {
        await using var db = _factory.Criar();
        var usuario = new FakeUsuarioAtual();
        var colaboradores = new ColaboradorService(db, usuario, new AuditoriaService(db, usuario));

        await colaboradores.CriarAsync(new Colaborador
        {
            NomeCompleto = "Fulano de Tal",
            Matricula = "0002",
            Email = "fulano2@elcop.com.br",
            Telefone = valorPerigoso
        });

        var relatorios = CriarServico(db);
        var csv = DecodificarCsv(await relatorios.ExportarColaboradoresAsync(new ColaboradorFiltro()));

        Assert.Contains("'" + valorPerigoso, csv);
    }

    [Fact]
    public async Task ExportarColaboradoresAsync_CampoNormal_NaoRecebePrefixo()
    {
        await using var db = _factory.Criar();
        var usuario = new FakeUsuarioAtual();
        var colaboradores = new ColaboradorService(db, usuario, new AuditoriaService(db, usuario));

        await colaboradores.CriarAsync(new Colaborador
        {
            NomeCompleto = "Fulano de Tal",
            Matricula = "0003",
            Email = "fulano3@elcop.com.br",
            Cargo = "Analista de Suporte"
        });

        var relatorios = CriarServico(db);
        var csv = DecodificarCsv(await relatorios.ExportarColaboradoresAsync(new ColaboradorFiltro()));

        Assert.Contains("Analista de Suporte", csv);
        Assert.DoesNotContain("'Analista", csv);
    }

    [Fact]
    public async Task ExportarAtivosAsync_ComBaseVazia_GeraSomenteCabecalho()
    {
        await using var db = _factory.Criar();
        var relatorios = CriarServico(db);

        var csv = DecodificarCsv(await relatorios.ExportarAtivosAsync(new AtivoFiltro()));
        var linhas = csv.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

        Assert.Single(linhas);
        Assert.StartsWith("Patrimônio;", linhas[0]);
    }
}
