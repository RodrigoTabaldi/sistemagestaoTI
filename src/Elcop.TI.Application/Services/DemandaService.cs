using Elcop.TI.Application.Common;
using Elcop.TI.Application.Models;
using Elcop.TI.Domain.Common;
using Elcop.TI.Domain.Entities;
using Elcop.TI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Elcop.TI.Application.Services;

/// <summary>
/// Ciclo de vida das demandas de TI: abertura, acompanhamento e encerramento,
/// sempre deixando rastro na linha do tempo.
/// </summary>
public class DemandaService : IDemandaService
{
    /// <summary>Colunas exibidas no quadro, na ordem do fluxo de trabalho.</summary>
    private static readonly StatusDemanda[] ColunasQuadro =
    {
        StatusDemanda.Aberta,
        StatusDemanda.EmAndamento,
        StatusDemanda.AguardandoTerceiros,
        StatusDemanda.Pausada,
        StatusDemanda.Concluida
    };

    private readonly IAppDbContext _db;
    private readonly IUsuarioAtual _usuario;
    private readonly IAuditoriaService _auditoria;

    public DemandaService(IAppDbContext db, IUsuarioAtual usuario, IAuditoriaService auditoria)
    {
        _db = db;
        _usuario = usuario;
        _auditoria = auditoria;
    }

    public async Task<ResultadoPaginado<Demanda>> ListarAsync(
        DemandaFiltro filtro, CancellationToken ct = default)
    {
        var consulta = AplicarFiltro(filtro);

        consulta = filtro.Ordenacao switch
        {
            "prioridade" => consulta.OrderByDescending(d => d.Prioridade).ThenBy(d => d.DataAbertura),
            "prazo" => consulta.OrderBy(d => d.PrazoLimite ?? DateTime.MaxValue),
            "antigas" => consulta.OrderBy(d => d.DataAbertura),
            "status" => consulta.OrderBy(d => d.Status).ThenByDescending(d => d.Prioridade),
            _ => consulta.OrderByDescending(d => d.DataAbertura).ThenByDescending(d => d.Id)
        };

        return await consulta.PaginarAsync(filtro.Pagina, filtro.TamanhoPagina, ct);
    }

    public async Task<QuadroKanban> ObterQuadroAsync(DemandaFiltro filtro, CancellationToken ct = default)
    {
        // O quadro ignora o recorte por status (cada coluna é um status) e traz um
        // teto por coluna para não estourar a tela em bases grandes.
        var filtroQuadro = filtro;
        var demandas = await AplicarFiltro(filtroQuadro, ignorarStatus: true)
            .Where(d => d.Status != StatusDemanda.Cancelada)
            .OrderBy(d => d.Ordem)
            .ThenByDescending(d => d.Prioridade)
            .ThenBy(d => d.PrazoLimite ?? DateTime.MaxValue)
            .Take(400)
            .ToListAsync(ct);

        var colunas = ColunasQuadro
            .Select(status => new ColunaKanban
            {
                Status = status,
                Titulo = status.ObterNome(),
                Demandas = demandas.Where(d => d.Status == status).ToList()
            })
            .ToList();

        return new QuadroKanban { Colunas = colunas, Filtro = filtro };
    }

    public Task<Demanda?> ObterAsync(int id, CancellationToken ct = default) =>
        _db.Demandas.FirstOrDefaultAsync(d => d.Id == id, ct);

    public Task<Demanda?> ObterCompletaAsync(int id, CancellationToken ct = default) =>
        _db.Demandas
            .AsNoTracking()
            .Include(d => d.Solicitante).ThenInclude(c => c!.Departamento)
            .Include(d => d.Departamento)
            .Include(d => d.Ativo)
            .Include(d => d.Andamentos.OrderByDescending(a => a.Data))
            .FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<int> CriarAsync(Demanda demanda, CancellationToken ct = default)
    {
        // Só o título é exigido: tudo o mais que ficar em branco recebe aqui um padrão
        // coerente (código, data de abertura, SLA da prioridade, posição no quadro).
        Normalizar(demanda);

        demanda.Codigo = await GerarCodigoAsync(ct);
        demanda.DataAbertura = demanda.DataAbertura == default ? DateTime.Now : demanda.DataAbertura;
        demanda.PrazoLimite ??= Demanda.CalcularPrazoSugerido(demanda.Prioridade, demanda.DataAbertura);
        demanda.Ordem = await ProximaOrdemAsync(demanda.Status, ct);
        demanda.MarcarCriacao(_usuario.NomeExibicao);

        if (demanda.Status == StatusDemanda.EmAndamento)
            demanda.DataInicio = DateTime.Now;

        // Herda o departamento do solicitante quando não informado.
        if (demanda.DepartamentoId is null && demanda.SolicitanteId.HasValue)
        {
            demanda.DepartamentoId = await _db.Colaboradores
                .Where(c => c.Id == demanda.SolicitanteId)
                .Select(c => c.DepartamentoId)
                .FirstOrDefaultAsync(ct);
        }

        _db.Demandas.Add(demanda);

        demanda.Andamentos.Add(new DemandaAndamento
        {
            Descricao = "Demanda registrada no sistema.",
            Autor = _usuario.NomeExibicao,
            Data = DateTime.Now,
            StatusNovo = demanda.Status,
            Automatico = true
        });

        await _auditoria.RegistrarAsync(
            TipoAcaoAuditoria.Criacao, nameof(Demanda), null,
            $"Demanda {demanda.Codigo} aberta: {demanda.Titulo}", ct);

        await _db.SaveChangesAsync(ct);
        return demanda.Id;
    }

    public async Task AtualizarAsync(Demanda demanda, CancellationToken ct = default)
    {
        Normalizar(demanda);

        var existente = await _db.Demandas.FirstOrDefaultAsync(d => d.Id == demanda.Id, ct)
            ?? throw new RegraDeNegocioException("Demanda não encontrada.");

        var statusAnterior = existente.Status;

        var codigo = existente.Codigo;
        var criadoEm = existente.CriadoEm;
        var criadoPor = existente.CriadoPor;
        var excluido = existente.Excluido;
        var ordem = existente.Ordem;
        var tempoGasto = existente.TempoGastoMinutos;

        _db.Demandas.Entry(existente).CurrentValues.SetValues(demanda);

        existente.Codigo = codigo;
        existente.CriadoEm = criadoEm;
        existente.CriadoPor = criadoPor;
        existente.Excluido = excluido;
        existente.Ordem = ordem;
        existente.TempoGastoMinutos = tempoGasto;
        existente.MarcarAtualizacao(_usuario.NomeExibicao);

        if (statusAnterior != demanda.Status)
        {
            existente.Status = statusAnterior;   // AplicarStatus precisa enxergar o estado antigo
            existente.AplicarStatus(demanda.Status);

            existente.Andamentos.Add(new DemandaAndamento
            {
                Descricao = $"Status alterado de \"{statusAnterior.ObterNome()}\" " +
                            $"para \"{demanda.Status.ObterNome()}\".",
                Autor = _usuario.NomeExibicao,
                Data = DateTime.Now,
                StatusAnterior = statusAnterior,
                StatusNovo = demanda.Status,
                Automatico = true
            });
        }

        await _auditoria.RegistrarAsync(
            TipoAcaoAuditoria.Alteracao, nameof(Demanda), demanda.Id,
            $"Demanda {existente.Codigo} atualizada.", ct);

        await _db.SaveChangesAsync(ct);
    }

    public async Task ExcluirAsync(int id, CancellationToken ct = default)
    {
        var demanda = await _db.Demandas.FirstOrDefaultAsync(d => d.Id == id, ct)
            ?? throw new RegraDeNegocioException("Demanda não encontrada.");

        demanda.Excluido = true;
        demanda.MarcarAtualizacao(_usuario.NomeExibicao);

        await _auditoria.RegistrarAsync(
            TipoAcaoAuditoria.Exclusao, nameof(Demanda), id,
            $"Demanda {demanda.Codigo} excluída.", ct);

        await _db.SaveChangesAsync(ct);
    }

    public async Task AdicionarAndamentoAsync(NovoAndamentoModel model, CancellationToken ct = default)
    {
        var demanda = await _db.Demandas
            .Include(d => d.Andamentos)
            .FirstOrDefaultAsync(d => d.Id == model.DemandaId, ct)
            ?? throw new RegraDeNegocioException("Demanda não encontrada.");

        var statusAnterior = demanda.Status;
        var mudouStatus = model.NovoStatus.HasValue && model.NovoStatus.Value != statusAnterior;

        demanda.Andamentos.Add(new DemandaAndamento
        {
            Descricao = model.Descricao.Trim(),
            Autor = _usuario.NomeExibicao,
            Data = DateTime.Now,
            StatusAnterior = mudouStatus ? statusAnterior : null,
            StatusNovo = mudouStatus ? model.NovoStatus : null,
            TempoGastoMinutos = model.TempoGastoMinutos,
            PercentualInformado = model.PercentualConclusao
        });

        demanda.TempoGastoMinutos += model.TempoGastoMinutos;

        if (model.PercentualConclusao.HasValue)
            demanda.PercentualConclusao = Math.Clamp(model.PercentualConclusao.Value, 0, 100);

        if (mudouStatus)
            demanda.AplicarStatus(model.NovoStatus!.Value);

        demanda.MarcarAtualizacao(_usuario.NomeExibicao);

        await _auditoria.RegistrarAsync(
            TipoAcaoAuditoria.Alteracao, nameof(Demanda), demanda.Id,
            mudouStatus
                ? $"Demanda {demanda.Codigo}: {statusAnterior.ObterNome()} → {demanda.Status.ObterNome()}."
                : $"Novo andamento na demanda {demanda.Codigo}.", ct);

        await _db.SaveChangesAsync(ct);
    }

    public async Task MoverAsync(
        int id, StatusDemanda novoStatus, int? novaOrdem, CancellationToken ct = default)
    {
        var demanda = await _db.Demandas
            .Include(d => d.Andamentos)
            .FirstOrDefaultAsync(d => d.Id == id, ct)
            ?? throw new RegraDeNegocioException("Demanda não encontrada.");

        var statusAnterior = demanda.Status;
        demanda.Ordem = novaOrdem ?? demanda.Ordem;

        if (statusAnterior != novoStatus)
        {
            demanda.AplicarStatus(novoStatus);
            demanda.Andamentos.Add(new DemandaAndamento
            {
                Descricao = $"Movida no quadro: \"{statusAnterior.ObterNome()}\" → \"{novoStatus.ObterNome()}\".",
                Autor = _usuario.NomeExibicao,
                Data = DateTime.Now,
                StatusAnterior = statusAnterior,
                StatusNovo = novoStatus,
                Automatico = true
            });

            await _auditoria.RegistrarAsync(
                TipoAcaoAuditoria.Alteracao, nameof(Demanda), id,
                $"Demanda {demanda.Codigo} movida para \"{novoStatus.ObterNome()}\".", ct);
        }

        demanda.MarcarAtualizacao(_usuario.NomeExibicao);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<string>> ListarResponsaveisAsync(CancellationToken ct = default) =>
        await _db.Demandas
            .AsNoTracking()
            .Where(d => d.Responsavel != null && d.Responsavel != "")
            .Select(d => d.Responsavel!)
            .Distinct()
            .OrderBy(r => r)
            .ToListAsync(ct);

    public async Task<ContadoresDemanda> ObterContadoresAsync(CancellationToken ct = default)
    {
        var hoje = DateTime.Today;
        var inicioMes = new DateTime(hoje.Year, hoje.Month, 1);

        var resumo = await _db.Demandas
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Abertas = g.Count(d =>
                    d.Status != StatusDemanda.Concluida && d.Status != StatusDemanda.Cancelada),
                EmAndamento = g.Count(d => d.Status == StatusDemanda.EmAndamento),
                Atrasadas = g.Count(d =>
                    d.PrazoLimite != null && d.PrazoLimite < hoje
                    && d.Status != StatusDemanda.Concluida && d.Status != StatusDemanda.Cancelada),
                ConcluidasNoMes = g.Count(d =>
                    d.Status == StatusDemanda.Concluida && d.DataConclusao >= inicioMes),
                Criticas = g.Count(d =>
                    d.Prioridade == PrioridadeDemanda.Critica
                    && d.Status != StatusDemanda.Concluida && d.Status != StatusDemanda.Cancelada)
            })
            .FirstOrDefaultAsync(ct);

        return resumo is null
            ? new ContadoresDemanda(0, 0, 0, 0, 0)
            : new ContadoresDemanda(
                resumo.Abertas, resumo.EmAndamento, resumo.Atrasadas,
                resumo.ConcluidasNoMes, resumo.Criticas);
    }

    private IQueryable<Demanda> AplicarFiltro(DemandaFiltro filtro, bool ignorarStatus = false)
    {
        var consulta = _db.Demandas
            .AsNoTracking()
            .Include(d => d.Solicitante)
            .Include(d => d.Departamento)
            .Include(d => d.Ativo)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filtro.Busca))
        {
            var termo = filtro.Busca.Trim();
            consulta = consulta.Where(d =>
                EF.Functions.Like(d.Codigo, $"%{termo}%") ||
                EF.Functions.Like(d.Titulo, $"%{termo}%") ||
                (d.Descricao != null && EF.Functions.Like(d.Descricao, $"%{termo}%")) ||
                (d.Tags != null && EF.Functions.Like(d.Tags, $"%{termo}%")) ||
                (d.Responsavel != null && EF.Functions.Like(d.Responsavel, $"%{termo}%")) ||
                (d.Solicitante != null && EF.Functions.Like(d.Solicitante.NomeCompleto, $"%{termo}%")));
        }

        if (!ignorarStatus && filtro.Status.HasValue)
            consulta = consulta.Where(d => d.Status == filtro.Status);

        if (filtro.Prioridade.HasValue) consulta = consulta.Where(d => d.Prioridade == filtro.Prioridade);
        if (filtro.Categoria.HasValue) consulta = consulta.Where(d => d.Categoria == filtro.Categoria);
        if (filtro.DepartamentoId.HasValue) consulta = consulta.Where(d => d.DepartamentoId == filtro.DepartamentoId);
        if (filtro.SolicitanteId.HasValue) consulta = consulta.Where(d => d.SolicitanteId == filtro.SolicitanteId);

        if (!string.IsNullOrWhiteSpace(filtro.Responsavel))
            consulta = consulta.Where(d => d.Responsavel == filtro.Responsavel);

        if (!filtro.IncluirEncerradas)
            consulta = consulta.Where(d =>
                d.Status != StatusDemanda.Concluida && d.Status != StatusDemanda.Cancelada);

        if (filtro.SomenteAtrasadas)
            consulta = consulta.Where(d =>
                d.PrazoLimite != null
                && d.PrazoLimite < DateTime.Today
                && d.Status != StatusDemanda.Concluida
                && d.Status != StatusDemanda.Cancelada);

        return consulta;
    }

    /// <summary>
    /// Apara os textos e traduz "não informado" para o valor que o resto do sistema espera:
    /// descrição vazia (a coluna é NOT NULL) e nulo nos campos opcionais, para que um campo
    /// deixado em branco no formulário não vire espaço em branco no banco nem apareça como
    /// responsável/etiqueta fantasma nas listagens.
    /// </summary>
    private static void Normalizar(Demanda demanda)
    {
        demanda.Titulo = demanda.Titulo?.Trim() ?? string.Empty;

        // A validação do formulário já barra isto; aqui vale para qualquer outro chamador.
        if (string.IsNullOrWhiteSpace(demanda.Titulo))
            throw new RegraDeNegocioException("Informe o título da demanda.");

        demanda.Descricao = demanda.Descricao?.Trim() ?? string.Empty;
        demanda.Responsavel = SeVazioNulo(demanda.Responsavel);
        demanda.Tags = SeVazioNulo(demanda.Tags);
        demanda.Solucao = SeVazioNulo(demanda.Solucao);

        static string? SeVazioNulo(string? valor) =>
            string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    }

    private async Task<int> ProximaOrdemAsync(StatusDemanda status, CancellationToken ct)
    {
        var maior = await _db.Demandas
            .Where(d => d.Status == status)
            .Select(d => (int?)d.Ordem)
            .MaxAsync(ct);

        return (maior ?? 0) + 1;
    }

    private async Task<string> GerarCodigoAsync(CancellationToken ct)
    {
        var ano = DateTime.Now.Year;
        var prefixo = $"DEM-{ano}-";

        var sequencia = await _db.Demandas
            .IgnoreQueryFilters()
            .CountAsync(d => d.Codigo.StartsWith(prefixo), ct);

        string codigo;
        do
        {
            sequencia++;
            codigo = $"{prefixo}{sequencia:D4}";
        }
        while (await _db.Demandas.IgnoreQueryFilters().AnyAsync(d => d.Codigo == codigo, ct));

        return codigo;
    }
}
