using Elcop.TI.Domain.Entities;
using Elcop.TI.Domain.Enums;

namespace Elcop.TI.Application.Models;

/// <summary>Ponto de uma série exibida nos gráficos do painel.</summary>
public record ItemGrafico(string Rotulo, decimal Valor, string? Cor = null, string? Chave = null);

/// <summary>Alerta acionável exibido no painel (garantia, atraso, estoque baixo…).</summary>
public record AlertaPainel(string Titulo, string Descricao, string Severidade, string Icone, string? Url);

/// <summary>Consolidação de indicadores apresentada na tela inicial.</summary>
public class PainelModel
{
    // ---------- Inventário ----------
    public int TotalAtivos { get; set; }
    public int AtivosDisponiveis { get; set; }
    public int AtivosEmUso { get; set; }
    public int AtivosEmManutencao { get; set; }
    public int AtivosBaixados { get; set; }
    public decimal ValorInventario { get; set; }

    // ---------- Pessoas ----------
    public int TotalColaboradores { get; set; }
    public int ColaboradoresComAtivos { get; set; }

    // ---------- Movimentações ----------
    public int MovimentacoesEmAberto { get; set; }
    public int DevolucoesAtrasadas { get; set; }
    public int MovimentacoesNoMes { get; set; }

    // ---------- Demandas ----------
    public int DemandasAbertas { get; set; }
    public int DemandasEmAndamento { get; set; }
    public int DemandasAtrasadas { get; set; }
    public int DemandasConcluidasNoMes { get; set; }
    public int DemandasCriticasAbertas { get; set; }
    public double TempoMedioAtendimentoHoras { get; set; }

    // ---------- Séries ----------
    public IReadOnlyList<ItemGrafico> AtivosPorTipo { get; set; } = Array.Empty<ItemGrafico>();
    public IReadOnlyList<ItemGrafico> AtivosPorStatus { get; set; } = Array.Empty<ItemGrafico>();
    public IReadOnlyList<ItemGrafico> DemandasPorStatus { get; set; } = Array.Empty<ItemGrafico>();
    public IReadOnlyList<ItemGrafico> DemandasPorPrioridade { get; set; } = Array.Empty<ItemGrafico>();
    public IReadOnlyList<ItemGrafico> AtivosPorDepartamento { get; set; } = Array.Empty<ItemGrafico>();
    public IReadOnlyList<ItemGrafico> MovimentacoesPorMes { get; set; } = Array.Empty<ItemGrafico>();
    public IReadOnlyList<ItemGrafico> DemandasPorMes { get; set; } = Array.Empty<ItemGrafico>();

    // ---------- Listas ----------
    public IReadOnlyList<Movimentacao> UltimasMovimentacoes { get; set; } = Array.Empty<Movimentacao>();
    public IReadOnlyList<Movimentacao> DevolucoesPendentes { get; set; } = Array.Empty<Movimentacao>();
    public IReadOnlyList<Demanda> DemandasPrioritarias { get; set; } = Array.Empty<Demanda>();
    public IReadOnlyList<Ativo> GarantiasVencendo { get; set; } = Array.Empty<Ativo>();
    public IReadOnlyList<AlertaPainel> Alertas { get; set; } = Array.Empty<AlertaPainel>();

    /// <summary>Percentual do parque atualmente entregue a colaboradores.</summary>
    public double TaxaUtilizacao => TotalAtivos == 0
        ? 0
        : Math.Round(AtivosEmUso * 100d / TotalAtivos, 1);

    public double TaxaDisponibilidade => TotalAtivos == 0
        ? 0
        : Math.Round(AtivosDisponiveis * 100d / TotalAtivos, 1);
}

/// <summary>Resumo do parque por tipo, usado na tela de relatórios.</summary>
public record ResumoPorTipo(
    TipoAtivo Tipo,
    string Descricao,
    int Total,
    int Disponiveis,
    int EmUso,
    int EmManutencao,
    decimal ValorTotal);
