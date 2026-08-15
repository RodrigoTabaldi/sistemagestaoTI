using Elcop.TI.Application.Common;
using Elcop.TI.Application.Models;
using Elcop.TI.Domain.Common;
using Elcop.TI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Elcop.TI.Application.Services;

/// <summary>
/// Consolida os números do inventário e das demandas para o painel inicial.
/// </summary>
public class PainelService : IPainelService
{
    /// <summary>Paleta alinhada à identidade ELCOP (vinho + grafite) para as séries.</summary>
    private static readonly IReadOnlyDictionary<StatusAtivo, string> CoresStatusAtivo =
        new Dictionary<StatusAtivo, string>
        {
            [StatusAtivo.Disponivel] = "#2E9E6B",
            [StatusAtivo.EmUso] = "#8B1F26",
            [StatusAtivo.Reservado] = "#B98A2E",
            [StatusAtivo.EmManutencao] = "#C4741C",
            [StatusAtivo.Emprestado] = "#3F6FB5",
            [StatusAtivo.Extraviado] = "#9C2B7A",
            [StatusAtivo.Danificado] = "#B4433C",
            [StatusAtivo.Baixado] = "#6B6B6B"
        };

    private static readonly IReadOnlyDictionary<StatusDemanda, string> CoresStatusDemanda =
        new Dictionary<StatusDemanda, string>
        {
            [StatusDemanda.Aberta] = "#3F6FB5",
            [StatusDemanda.EmAndamento] = "#8B1F26",
            [StatusDemanda.AguardandoTerceiros] = "#B98A2E",
            [StatusDemanda.Pausada] = "#6B6B6B",
            [StatusDemanda.Concluida] = "#2E9E6B",
            [StatusDemanda.Cancelada] = "#9A9A9A"
        };

    private static readonly IReadOnlyDictionary<PrioridadeDemanda, string> CoresPrioridade =
        new Dictionary<PrioridadeDemanda, string>
        {
            [PrioridadeDemanda.Baixa] = "#2E9E6B",
            [PrioridadeDemanda.Media] = "#3F6FB5",
            [PrioridadeDemanda.Alta] = "#C4741C",
            [PrioridadeDemanda.Critica] = "#8B1F26"
        };

    private readonly IAppDbContext _db;
    private readonly ICacheService _cache;

    public PainelService(IAppDbContext db, ICacheService cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<PainelModel> ObterAsync(CancellationToken ct = default) =>
        await _cache.ObterOuCriarAsync(
            "painel",
            TimeSpan.FromSeconds(60),
            () => CalcularPainelAsync(ct),
            ct);

    private async Task<PainelModel> CalcularPainelAsync(CancellationToken ct)
    {
        var hoje = DateTime.Today;
        var inicioMes = new DateTime(hoje.Year, hoje.Month, 1);
        var inicioSerie = inicioMes.AddMonths(-5);
        var limiteGarantia = hoje.AddDays(60);

        var painel = new PainelModel();

        // ---------- Inventário ----------
        var ativos = await _db.Ativos
            .AsNoTracking()
            .GroupBy(a => a.Status)
            // SQLite não agrega decimal: soma em double e converte de volta na aplicação.
            .Select(g => new { Status = g.Key, Total = g.Count(), Valor = g.Sum(a => (double)(a.ValorAquisicao ?? 0m)) })
            .ToListAsync(ct);

        painel.TotalAtivos = ativos.Sum(a => a.Total);
        painel.ValorInventario = (decimal)ativos.Sum(a => a.Valor);
        painel.AtivosDisponiveis = ativos.FirstOrDefault(a => a.Status == StatusAtivo.Disponivel)?.Total ?? 0;
        painel.AtivosEmManutencao = ativos.FirstOrDefault(a => a.Status == StatusAtivo.EmManutencao)?.Total ?? 0;
        painel.AtivosBaixados = ativos.FirstOrDefault(a => a.Status == StatusAtivo.Baixado)?.Total ?? 0;
        painel.AtivosEmUso = ativos
            .Where(a => a.Status is StatusAtivo.EmUso or StatusAtivo.Emprestado)
            .Sum(a => a.Total);

        painel.AtivosPorStatus = ativos
            .OrderByDescending(a => a.Total)
            .Select(a => new ItemGrafico(
                a.Status.ObterNome(), a.Total, CoresStatusAtivo.GetValueOrDefault(a.Status, "#8B1F26"),
                a.Status.ToString()))
            .ToList();

        var porTipo = await _db.Ativos
            .AsNoTracking()
            .GroupBy(a => a.Tipo)
            .Select(g => new { Tipo = g.Key, Total = g.Count() })
            .ToListAsync(ct);

        painel.AtivosPorTipo = porTipo
            .OrderByDescending(t => t.Total)
            .Take(10)
            .Select(t => new ItemGrafico(t.Tipo.ObterNome(), t.Total, null, t.Tipo.ToString()))
            .ToList();

        var porDepartamento = await _db.Ativos
            .AsNoTracking()
            .Where(a => a.DepartamentoId != null)
            .GroupBy(a => a.Departamento!.Nome)
            .Select(g => new { Nome = g.Key, Total = g.Count() })
            .OrderByDescending(g => g.Total)
            .Take(8)
            .ToListAsync(ct);

        painel.AtivosPorDepartamento = porDepartamento
            .Select(d => new ItemGrafico(d.Nome, d.Total))
            .ToList();

        // ---------- Pessoas ----------
        painel.TotalColaboradores = await _db.Colaboradores
            .CountAsync(c => c.Status != StatusColaborador.Desligado, ct);

        painel.ColaboradoresComAtivos = await _db.Ativos
            .Where(a => a.ColaboradorAtualId != null)
            .Select(a => a.ColaboradorAtualId)
            .Distinct()
            .CountAsync(ct);

        // ---------- Movimentações ----------
        var emAberto = _db.Movimentacoes.Where(m =>
            m.Status == StatusMovimentacao.EmAberto || m.Status == StatusMovimentacao.Atrasado);

        painel.MovimentacoesEmAberto = await emAberto.CountAsync(ct);
        painel.DevolucoesAtrasadas = await emAberto
            .CountAsync(m => m.DataPrevistaDevolucao != null && m.DataPrevistaDevolucao < hoje, ct);
        painel.MovimentacoesNoMes = await _db.Movimentacoes
            .CountAsync(m => m.DataRetirada >= inicioMes, ct);

        painel.UltimasMovimentacoes = await _db.Movimentacoes
            .AsNoTracking()
            .Include(m => m.Ativo)
            .Include(m => m.Colaborador)
            .OrderByDescending(m => m.Id)
            .Take(8)
            .ToListAsync(ct);

        painel.DevolucoesPendentes = await emAberto
            .AsNoTracking()
            .Include(m => m.Ativo)
            .Include(m => m.Colaborador)
            .Where(m => m.DataPrevistaDevolucao != null)
            .OrderBy(m => m.DataPrevistaDevolucao)
            .Take(6)
            .ToListAsync(ct);

        // ---------- Demandas ----------
        var demandasPorStatus = await _db.Demandas
            .AsNoTracking()
            .GroupBy(d => d.Status)
            .Select(g => new { Status = g.Key, Total = g.Count() })
            .ToListAsync(ct);

        painel.DemandasAbertas = demandasPorStatus
            .Where(d => d.Status is StatusDemanda.Aberta or StatusDemanda.AguardandoTerceiros or StatusDemanda.Pausada)
            .Sum(d => d.Total);
        painel.DemandasEmAndamento = demandasPorStatus
            .FirstOrDefault(d => d.Status == StatusDemanda.EmAndamento)?.Total ?? 0;

        painel.DemandasPorStatus = demandasPorStatus
            .OrderBy(d => d.Status)
            .Select(d => new ItemGrafico(
                d.Status.ObterNome(), d.Total, CoresStatusDemanda.GetValueOrDefault(d.Status, "#8B1F26"),
                d.Status.ToString()))
            .ToList();

        var demandasPorPrioridade = await _db.Demandas
            .AsNoTracking()
            .Where(d => d.Status != StatusDemanda.Concluida && d.Status != StatusDemanda.Cancelada)
            .GroupBy(d => d.Prioridade)
            .Select(g => new { Prioridade = g.Key, Total = g.Count() })
            .ToListAsync(ct);

        painel.DemandasPorPrioridade = demandasPorPrioridade
            .OrderByDescending(d => d.Prioridade)
            .Select(d => new ItemGrafico(
                d.Prioridade.ObterNome(), d.Total, CoresPrioridade.GetValueOrDefault(d.Prioridade, "#8B1F26"),
                d.Prioridade.ToString()))
            .ToList();

        painel.DemandasCriticasAbertas = demandasPorPrioridade
            .FirstOrDefault(d => d.Prioridade == PrioridadeDemanda.Critica)?.Total ?? 0;

        painel.DemandasAtrasadas = await _db.Demandas.CountAsync(d =>
            d.PrazoLimite != null && d.PrazoLimite < hoje
            && d.Status != StatusDemanda.Concluida && d.Status != StatusDemanda.Cancelada, ct);

        painel.DemandasConcluidasNoMes = await _db.Demandas.CountAsync(d =>
            d.Status == StatusDemanda.Concluida && d.DataConclusao != null && d.DataConclusao >= inicioMes, ct);

        var concluidas = await _db.Demandas
            .AsNoTracking()
            .Where(d => d.DataConclusao != null && d.DataConclusao >= inicioSerie)
            .Select(d => new { d.DataAbertura, d.DataConclusao })
            .ToListAsync(ct);

        painel.TempoMedioAtendimentoHoras = concluidas.Count == 0
            ? 0
            : Math.Round(concluidas.Average(d => (d.DataConclusao!.Value - d.DataAbertura).TotalHours), 1);

        painel.DemandasPrioritarias = await _db.Demandas
            .AsNoTracking()
            .Include(d => d.Solicitante)
            .Where(d => d.Status != StatusDemanda.Concluida && d.Status != StatusDemanda.Cancelada)
            .OrderByDescending(d => d.Prioridade)
            .ThenBy(d => d.PrazoLimite ?? DateTime.MaxValue)
            .Take(6)
            .ToListAsync(ct);

        // ---------- Séries mensais (últimos 6 meses) ----------
        var movimentacoesRecentes = await _db.Movimentacoes
            .AsNoTracking()
            .Where(m => m.DataRetirada >= inicioSerie)
            .Select(m => m.DataRetirada)
            .ToListAsync(ct);

        var demandasRecentes = await _db.Demandas
            .AsNoTracking()
            .Where(d => d.DataAbertura >= inicioSerie)
            .Select(d => d.DataAbertura)
            .ToListAsync(ct);

        painel.MovimentacoesPorMes = MontarSerieMensal(movimentacoesRecentes, inicioSerie);
        painel.DemandasPorMes = MontarSerieMensal(demandasRecentes, inicioSerie);

        // ---------- Garantias e alertas ----------
        painel.GarantiasVencendo = await _db.Ativos
            .AsNoTracking()
            .Where(a => a.GarantiaAte != null && a.GarantiaAte >= hoje && a.GarantiaAte <= limiteGarantia)
            .OrderBy(a => a.GarantiaAte)
            .Take(6)
            .ToListAsync(ct);

        painel.Alertas = MontarAlertas(painel);

        return painel;
    }

    private static IReadOnlyList<ItemGrafico> MontarSerieMensal(
        IEnumerable<DateTime> datas, DateTime inicio)
    {
        var agrupado = datas
            .GroupBy(d => new DateTime(d.Year, d.Month, 1))
            .ToDictionary(g => g.Key, g => g.Count());

        // Meses sem registro precisam aparecer como zero para a linha não "pular".
        return Enumerable.Range(0, 6)
            .Select(i =>
            {
                var mes = inicio.AddMonths(i);
                var rotulo = mes.ToString("MMM/yy", new System.Globalization.CultureInfo("pt-BR"));
                return new ItemGrafico(rotulo, agrupado.GetValueOrDefault(mes), null, mes.ToString("yyyy-MM"));
            })
            .ToList();
    }

    private static List<AlertaPainel> MontarAlertas(PainelModel painel)
    {
        var alertas = new List<AlertaPainel>();

        if (painel.DevolucoesAtrasadas > 0)
            alertas.Add(new AlertaPainel(
                $"{painel.DevolucoesAtrasadas} devolução(ões) em atraso",
                "Equipamentos com prazo de devolução vencido.",
                "critico", "alerta", "/Movimentacoes?SomenteAtrasadas=true"));

        if (painel.DemandasAtrasadas > 0)
            alertas.Add(new AlertaPainel(
                $"{painel.DemandasAtrasadas} demanda(s) fora do prazo",
                "Chamados que ultrapassaram o SLA definido.",
                "critico", "relogio", "/Demandas?SomenteAtrasadas=true"));

        if (painel.DemandasCriticasAbertas > 0)
            alertas.Add(new AlertaPainel(
                $"{painel.DemandasCriticasAbertas} demanda(s) crítica(s) em aberto",
                "Prioridade máxima aguardando tratativa.",
                "atencao", "raio", "/Demandas?Prioridade=Critica&IncluirEncerradas=false"));

        if (painel.GarantiasVencendo.Count > 0)
            alertas.Add(new AlertaPainel(
                $"{painel.GarantiasVencendo.Count} ativo(s) com garantia vencendo",
                "Garantias expiram nos próximos 60 dias.",
                "atencao", "escudo", "/Ativos?GarantiaVencendo=true"));

        if (painel.AtivosEmManutencao > 0)
            alertas.Add(new AlertaPainel(
                $"{painel.AtivosEmManutencao} ativo(s) em manutenção",
                "Equipamentos indisponíveis para entrega.",
                "info", "ferramenta", "/Ativos?Status=EmManutencao"));

        if (painel.TotalAtivos > 0 && painel.AtivosDisponiveis == 0)
            alertas.Add(new AlertaPainel(
                "Estoque sem itens disponíveis",
                "Nenhum ativo livre para novas entregas.",
                "critico", "caixa", "/Ativos"));

        return alertas;
    }
}
