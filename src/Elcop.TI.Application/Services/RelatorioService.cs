using System.Globalization;
using System.Text;
using Elcop.TI.Application.Models;
using Elcop.TI.Domain.Common;

namespace Elcop.TI.Application.Services;

/// <inheritdoc />
public class RelatorioService : IRelatorioService
{
    private static readonly CultureInfo Cultura = new("pt-BR");
    private const int LimiteExportacao = 10_000;

    private readonly IAtivoService _ativos;
    private readonly IColaboradorService _colaboradores;
    private readonly IMovimentacaoService _movimentacoes;
    private readonly IDemandaService _demandas;

    public RelatorioService(
        IAtivoService ativos,
        IColaboradorService colaboradores,
        IMovimentacaoService movimentacoes,
        IDemandaService demandas)
    {
        _ativos = ativos;
        _colaboradores = colaboradores;
        _movimentacoes = movimentacoes;
        _demandas = demandas;
    }

    public async Task<byte[]> ExportarAtivosAsync(AtivoFiltro filtro, CancellationToken ct = default)
    {
        var pagina = await _ativos.ListarAsync(ParaExportacao(filtro), ct);

        var csv = new CsvBuilder(
            "Patrimônio", "Tipo", "Marca", "Modelo", "Nº de série", "IMEI", "Linha",
            "Status", "Condição", "Departamento", "Localização", "Em posse de",
            "Aquisição", "Valor (R$)", "Nota fiscal", "Garantia até", "Hostname", "Observações");

        foreach (var a in pagina.Itens)
        {
            csv.AdicionarLinha(
                a.Patrimonio, a.Tipo.ObterNome(), a.Marca, a.Modelo, a.NumeroSerie, a.Imei, a.NumeroLinha,
                a.Status.ObterNome(), a.Condicao.ObterNome(), a.Departamento?.Nome, a.Localizacao?.Nome,
                a.ColaboradorAtual?.NomeCompleto,
                Data(a.DataAquisicao), Moeda(a.ValorAquisicao), a.NotaFiscal, Data(a.GarantiaAte),
                a.Hostname, a.Observacoes);
        }

        return csv.Gerar();
    }

    public async Task<byte[]> ExportarColaboradoresAsync(ColaboradorFiltro filtro, CancellationToken ct = default)
    {
        var pagina = await _colaboradores.ListarAsync(ParaExportacao(filtro), ct);

        var csv = new CsvBuilder(
            "Matrícula", "Nome completo", "E-mail", "Telefone", "Celular", "Cargo",
            "Departamento", "Localização", "Situação", "Admissão", "Gestor");

        foreach (var c in pagina.Itens)
        {
            csv.AdicionarLinha(
                c.Matricula, c.NomeCompleto, c.Email, c.Telefone, c.Celular, c.Cargo,
                c.Departamento?.Nome, c.Localizacao?.Nome, c.Status.ObterNome(),
                Data(c.DataAdmissao), c.GestorImediato);
        }

        return csv.Gerar();
    }

    public async Task<byte[]> ExportarMovimentacoesAsync(MovimentacaoFiltro filtro, CancellationToken ct = default)
    {
        var pagina = await _movimentacoes.ListarAsync(ParaExportacao(filtro), ct);

        var csv = new CsvBuilder(
            "Protocolo", "Tipo", "Situação", "Patrimônio", "Equipamento", "Nº de série",
            "Colaborador", "Matrícula", "Departamento", "Retirada", "Previsão devolução",
            "Devolução", "Dias em posse", "Condição retirada", "Condição devolução",
            "Responsável entrega", "Responsável recebimento", "Avaria");

        foreach (var m in pagina.Itens)
        {
            csv.AdicionarLinha(
                m.Protocolo, m.Tipo.ObterNome(), m.Status.ObterNome(),
                m.Ativo?.Patrimonio, m.Ativo?.DescricaoCurta, m.Ativo?.NumeroSerie,
                m.Colaborador?.NomeCompleto, m.Colaborador?.Matricula, m.Colaborador?.Departamento?.Nome,
                DataHora(m.DataRetirada), Data(m.DataPrevistaDevolucao), DataHora(m.DataDevolucao),
                m.DiasEmPosse.ToString(Cultura),
                m.CondicaoRetirada.ObterNome(), m.CondicaoDevolucao?.ObterNome(),
                m.ResponsavelEntrega, m.ResponsavelRecebimento, m.ComAvaria ? "Sim" : "Não");
        }

        return csv.Gerar();
    }

    public async Task<byte[]> ExportarDemandasAsync(DemandaFiltro filtro, CancellationToken ct = default)
    {
        var pagina = await _demandas.ListarAsync(ParaExportacao(filtro), ct);

        var csv = new CsvBuilder(
            "Código", "Título", "Categoria", "Prioridade", "Status", "Solicitante",
            "Departamento", "Responsável", "Ativo", "Abertura", "Prazo", "Conclusão",
            "Progresso (%)", "Tempo gasto (min)", "Etiquetas");

        foreach (var d in pagina.Itens)
        {
            csv.AdicionarLinha(
                d.Codigo, d.Titulo, d.Categoria.ObterNome(), d.Prioridade.ObterNome(), d.Status.ObterNome(),
                d.Solicitante?.NomeCompleto, d.Departamento?.Nome, d.Responsavel, d.Ativo?.Patrimonio,
                DataHora(d.DataAbertura), Data(d.PrazoLimite), DataHora(d.DataConclusao),
                d.PercentualConclusao.ToString(Cultura), d.TempoGastoMinutos.ToString(Cultura), d.Tags);
        }

        return csv.Gerar();
    }

    /// <summary>A exportação ignora a paginação da tela, respeitando apenas os filtros.</summary>
    private static T ParaExportacao<T>(T filtro) where T : FiltroBase
    {
        filtro.Pagina = 1;
        filtro.TamanhoPagina = LimiteExportacao;
        return filtro;
    }

    private static string Data(DateTime? valor) => valor?.ToString("dd/MM/yyyy", Cultura) ?? string.Empty;

    private static string DataHora(DateTime? valor) => valor?.ToString("dd/MM/yyyy HH:mm", Cultura) ?? string.Empty;

    private static string Moeda(decimal? valor) => valor?.ToString("N2", Cultura) ?? string.Empty;

    /// <summary>Montador de CSV com escape de aspas/quebras e separador ';'.</summary>
    private sealed class CsvBuilder
    {
        private readonly StringBuilder _conteudo = new();

        public CsvBuilder(params string[] cabecalhos) => AdicionarLinha(cabecalhos);

        public void AdicionarLinha(params string?[] campos) =>
            _conteudo.AppendLine(string.Join(';', campos.Select(Escapar)));

        public byte[] Gerar() =>
            // BOM UTF-8: sem ele o Excel pt-BR quebra os acentos.
            Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(_conteudo.ToString())).ToArray();

        /// <summary>Caracteres que o Excel/Sheets interpretam como início de fórmula.</summary>
        private static readonly char[] GatilhosDeFormula = ['=', '+', '-', '@', '\t', '\r'];

        private static string Escapar(string? campo)
        {
            if (string.IsNullOrEmpty(campo)) return string.Empty;

            var texto = campo.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ');

            // CSV formula injection: um campo vindo de cadastro (nome, observação, tags) que
            // comece com '=', '+', '-' ou '@' é executado como fórmula ao abrir no Excel/Sheets.
            // Um apóstrofo neutraliza sem alterar o valor visível.
            if (texto.Length > 0 && GatilhosDeFormula.Contains(texto[0]))
                texto = "'" + texto;

            return texto.Contains(';') || texto.Contains('"')
                ? $"\"{texto.Replace("\"", "\"\"")}\""
                : texto;
        }
    }
}
