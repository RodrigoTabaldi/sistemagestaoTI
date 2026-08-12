using Elcop.TI.Application.Common;
using Elcop.TI.Application.Models;
using Elcop.TI.Domain.Common;
using Elcop.TI.Domain.Entities;
using Elcop.TI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Elcop.TI.Application.Services;

/// <inheritdoc />
public class AtivoService : IAtivoService
{
    private const string PrefixoPatrimonio = "TI-";

    private readonly IAppDbContext _db;
    private readonly IUsuarioAtual _usuario;
    private readonly IAuditoriaService _auditoria;

    public AtivoService(IAppDbContext db, IUsuarioAtual usuario, IAuditoriaService auditoria)
    {
        _db = db;
        _usuario = usuario;
        _auditoria = auditoria;
    }

    public async Task<ResultadoPaginado<Ativo>> ListarAsync(AtivoFiltro filtro, CancellationToken ct = default)
    {
        var consulta = _db.Ativos
            .AsNoTracking()
            .Include(a => a.Departamento)
            .Include(a => a.Localizacao)
            .Include(a => a.ColaboradorAtual)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filtro.Busca))
        {
            var termo = filtro.Busca.Trim();
            consulta = consulta.Where(a =>
                EF.Functions.Like(a.Patrimonio, $"%{termo}%") ||
                EF.Functions.Like(a.Marca, $"%{termo}%") ||
                EF.Functions.Like(a.Modelo, $"%{termo}%") ||
                (a.NumeroSerie != null && EF.Functions.Like(a.NumeroSerie, $"%{termo}%")) ||
                (a.Imei != null && EF.Functions.Like(a.Imei, $"%{termo}%")) ||
                (a.Hostname != null && EF.Functions.Like(a.Hostname, $"%{termo}%")) ||
                (a.NumeroLinha != null && EF.Functions.Like(a.NumeroLinha, $"%{termo}%")) ||
                (a.ColaboradorAtual != null && EF.Functions.Like(a.ColaboradorAtual.NomeCompleto, $"%{termo}%")));
        }

        if (filtro.Tipo.HasValue) consulta = consulta.Where(a => a.Tipo == filtro.Tipo);
        if (filtro.Status.HasValue) consulta = consulta.Where(a => a.Status == filtro.Status);
        if (filtro.Condicao.HasValue) consulta = consulta.Where(a => a.Condicao == filtro.Condicao);
        if (filtro.DepartamentoId.HasValue) consulta = consulta.Where(a => a.DepartamentoId == filtro.DepartamentoId);
        if (filtro.LocalizacaoId.HasValue) consulta = consulta.Where(a => a.LocalizacaoId == filtro.LocalizacaoId);
        if (filtro.FornecedorId.HasValue) consulta = consulta.Where(a => a.FornecedorId == filtro.FornecedorId);
        if (filtro.ColaboradorId.HasValue) consulta = consulta.Where(a => a.ColaboradorAtualId == filtro.ColaboradorId);
        if (filtro.SemNumeroSerie) consulta = consulta.Where(a => a.NumeroSerie == null || a.NumeroSerie == "");

        if (filtro.GarantiaVencendo)
        {
            var limite = DateTime.Today.AddDays(60);
            consulta = consulta.Where(a =>
                a.GarantiaAte != null && a.GarantiaAte >= DateTime.Today && a.GarantiaAte <= limite);
        }

        consulta = filtro.Ordenacao switch
        {
            "patrimonio" => consulta.OrderBy(a => a.Patrimonio),
            "modelo" => consulta.OrderBy(a => a.Marca).ThenBy(a => a.Modelo),
            "tipo" => consulta.OrderBy(a => a.Tipo).ThenBy(a => a.Patrimonio),
            "status" => consulta.OrderBy(a => a.Status).ThenBy(a => a.Patrimonio),
            "valor" => consulta.OrderByDescending(a => a.ValorAquisicao),
            "antigos" => consulta.OrderBy(a => a.DataAquisicao ?? DateTime.MaxValue),
            _ => consulta.OrderByDescending(a => a.Id)
        };

        return await consulta.PaginarAsync(filtro.Pagina, filtro.TamanhoPagina, ct);
    }

    public Task<Ativo?> ObterAsync(int id, CancellationToken ct = default) =>
        _db.Ativos.FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<Ativo?> ObterCompletoAsync(int id, CancellationToken ct = default) =>
        _db.Ativos
            .AsNoTracking()
            .Include(a => a.Departamento)
            .Include(a => a.Localizacao)
            .Include(a => a.Fornecedor)
            .Include(a => a.ColaboradorAtual).ThenInclude(c => c!.Departamento)
            .Include(a => a.Movimentacoes.OrderByDescending(m => m.DataRetirada))
                .ThenInclude(m => m.Colaborador)
            .Include(a => a.Demandas.OrderByDescending(d => d.DataAbertura))
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<int> CriarAsync(Ativo ativo, CancellationToken ct = default)
    {
        await GarantirIdentificadoresUnicosAsync(ativo, ct);

        ativo.Patrimonio = ativo.Patrimonio.Trim().ToUpperInvariant();
        ativo.MarcarCriacao(_usuario.NomeExibicao);
        NormalizarPosse(ativo);

        _db.Ativos.Add(ativo);

        await _auditoria.RegistrarAsync(
            TipoAcaoAuditoria.Criacao, nameof(Ativo), null,
            $"Ativo {ativo.Patrimonio} ({ativo.DescricaoCurta}) cadastrado.", ct);

        await _db.SaveChangesAsync(ct);
        return ativo.Id;
    }

    public async Task AtualizarAsync(Ativo ativo, CancellationToken ct = default)
    {
        var existente = await _db.Ativos.FirstOrDefaultAsync(a => a.Id == ativo.Id, ct)
            ?? throw new RegraDeNegocioException("Ativo não encontrado.");

        await GarantirIdentificadoresUnicosAsync(ativo, ct);

        // Posse, exclusão lógica e trilha de criação são governadas pelo sistema,
        // nunca pelos campos que voltam do formulário.
        var colaboradorAtualId = existente.ColaboradorAtualId;
        var criadoEm = existente.CriadoEm;
        var criadoPor = existente.CriadoPor;
        var excluido = existente.Excluido;

        _db.Ativos.Entry(existente).CurrentValues.SetValues(ativo);

        existente.ColaboradorAtualId = colaboradorAtualId;
        existente.CriadoEm = criadoEm;
        existente.CriadoPor = criadoPor;
        existente.Excluido = excluido;
        existente.Patrimonio = ativo.Patrimonio.Trim().ToUpperInvariant();
        existente.MarcarAtualizacao(_usuario.NomeExibicao);

        await _auditoria.RegistrarAsync(
            TipoAcaoAuditoria.Alteracao, nameof(Ativo), ativo.Id,
            $"Ativo {existente.Patrimonio} atualizado.", ct);

        await _db.SaveChangesAsync(ct);
    }

    public async Task ExcluirAsync(int id, CancellationToken ct = default)
    {
        var ativo = await _db.Ativos.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new RegraDeNegocioException("Ativo não encontrado.");

        if (ativo.ColaboradorAtualId.HasValue)
            throw new RegraDeNegocioException(
                "Este ativo está em posse de um colaborador. Registre a devolução antes de excluí-lo.");

        ativo.Excluido = true;
        ativo.Status = StatusAtivo.Baixado;
        ativo.MarcarAtualizacao(_usuario.NomeExibicao);

        await _auditoria.RegistrarAsync(
            TipoAcaoAuditoria.Exclusao, nameof(Ativo), id,
            $"Ativo {ativo.Patrimonio} excluído do inventário.", ct);

        await _db.SaveChangesAsync(ct);
    }

    public async Task AlterarStatusAsync(int id, StatusAtivo status, string? motivo, CancellationToken ct = default)
    {
        var ativo = await _db.Ativos.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new RegraDeNegocioException("Ativo não encontrado.");

        if (ativo.ColaboradorAtualId.HasValue && status is StatusAtivo.Disponivel or StatusAtivo.Reservado)
            throw new RegraDeNegocioException(
                "O ativo está entregue a um colaborador. Registre a devolução para liberá-lo.");

        var anterior = ativo.Status;
        ativo.Status = status;
        ativo.MarcarAtualizacao(_usuario.NomeExibicao);

        if (!string.IsNullOrWhiteSpace(motivo))
            ativo.Observacoes = $"[{DateTime.Now:dd/MM/yyyy HH:mm}] {motivo}\n{ativo.Observacoes}".Trim();

        await _auditoria.RegistrarAsync(
            TipoAcaoAuditoria.Alteracao, nameof(Ativo), id,
            $"Status do ativo {ativo.Patrimonio}: {anterior.ObterNome()} → {status.ObterNome()}.", ct);

        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Movimentacao>> ObterHistoricoAsync(int ativoId, CancellationToken ct = default) =>
        await _db.Movimentacoes
            .AsNoTracking()
            .Include(m => m.Colaborador)
            .Where(m => m.AtivoId == ativoId)
            .OrderByDescending(m => m.DataRetirada)
            .ToListAsync(ct);

    public async Task<string> SugerirPatrimonioAsync(CancellationToken ct = default)
    {
        // Considera apenas patrimônios no padrão TI-000000 para calcular o próximo número.
        var numeros = await _db.Ativos
            .IgnoreQueryFilters()
            .Where(a => a.Patrimonio.StartsWith(PrefixoPatrimonio))
            .Select(a => a.Patrimonio.Substring(PrefixoPatrimonio.Length))
            .ToListAsync(ct);

        var maior = numeros
            .Select(n => int.TryParse(n, out var v) ? v : 0)
            .DefaultIfEmpty(0)
            .Max();

        return $"{PrefixoPatrimonio}{maior + 1:D6}";
    }

    public Task<bool> PatrimonioEmUsoAsync(string patrimonio, int? ignorarId = null, CancellationToken ct = default)
    {
        var valor = patrimonio.Trim().ToUpperInvariant();
        return _db.Ativos
            .IgnoreQueryFilters()
            .AnyAsync(a => a.Patrimonio.ToUpper() == valor && (ignorarId == null || a.Id != ignorarId), ct);
    }

    public Task<bool> NumeroSerieEmUsoAsync(string numeroSerie, int? ignorarId = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(numeroSerie)) return Task.FromResult(false);

        var valor = numeroSerie.Trim().ToUpperInvariant();
        return _db.Ativos
            .IgnoreQueryFilters()
            .AnyAsync(a => a.NumeroSerie != null
                        && a.NumeroSerie.ToUpper() == valor
                        && (ignorarId == null || a.Id != ignorarId), ct);
    }

    public async Task<IReadOnlyList<Ativo>> ListarDisponiveisAsync(int? incluirId = null, CancellationToken ct = default) =>
        await _db.Ativos
            .AsNoTracking()
            .Where(a => (a.ColaboradorAtualId == null
                         && (a.Status == StatusAtivo.Disponivel || a.Status == StatusAtivo.Reservado))
                        || (incluirId != null && a.Id == incluirId))
            .OrderBy(a => a.Tipo).ThenBy(a => a.Patrimonio)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ResumoPorTipo>> ResumirPorTipoAsync(CancellationToken ct = default)
    {
        var dados = await _db.Ativos
            .AsNoTracking()
            .GroupBy(a => a.Tipo)
            .Select(g => new
            {
                Tipo = g.Key,
                Total = g.Count(),
                Disponiveis = g.Count(a => a.Status == StatusAtivo.Disponivel),
                EmUso = g.Count(a => a.Status == StatusAtivo.EmUso),
                EmManutencao = g.Count(a => a.Status == StatusAtivo.EmManutencao),
                // SQLite não agrega decimal: soma em double e converte de volta na aplicação.
                Valor = g.Sum(a => (double)(a.ValorAquisicao ?? 0m))
            })
            .ToListAsync(ct);

        return dados
            .Select(d => new ResumoPorTipo(
                d.Tipo, d.Tipo.ObterNome(), d.Total, d.Disponiveis, d.EmUso, d.EmManutencao, (decimal)d.Valor))
            .OrderByDescending(r => r.Total)
            .ToList();
    }

    private async Task GarantirIdentificadoresUnicosAsync(Ativo ativo, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(ativo.Patrimonio))
            throw new RegraDeNegocioException("O número de patrimônio é obrigatório.");

        var id = ativo.Id == 0 ? (int?)null : ativo.Id;

        if (await PatrimonioEmUsoAsync(ativo.Patrimonio, id, ct))
            throw new RegraDeNegocioException(
                $"Já existe um ativo cadastrado com o patrimônio {ativo.Patrimonio.Trim().ToUpperInvariant()}.");

        if (!string.IsNullOrWhiteSpace(ativo.NumeroSerie)
            && await NumeroSerieEmUsoAsync(ativo.NumeroSerie, id, ct))
            throw new RegraDeNegocioException(
                $"O número de série {ativo.NumeroSerie.Trim()} já pertence a outro ativo.");
    }

    /// <summary>Um ativo recém-cadastrado nunca nasce entregue a alguém.</summary>
    private static void NormalizarPosse(Ativo ativo)
    {
        if (ativo.ColaboradorAtualId is null && ativo.Status == StatusAtivo.EmUso)
            ativo.Status = StatusAtivo.Disponivel;
    }
}
