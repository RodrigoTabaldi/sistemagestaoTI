using Elcop.TI.Application.Common;
using Elcop.TI.Application.Models;
using Elcop.TI.Domain.Common;
using Elcop.TI.Domain.Entities;
using Elcop.TI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Elcop.TI.Application.Services;

/// <inheritdoc />
public class ColaboradorService : IColaboradorService
{
    private readonly IAppDbContext _db;
    private readonly IUsuarioAtual _usuario;
    private readonly IAuditoriaService _auditoria;

    public ColaboradorService(IAppDbContext db, IUsuarioAtual usuario, IAuditoriaService auditoria)
    {
        _db = db;
        _usuario = usuario;
        _auditoria = auditoria;
    }

    public async Task<ResultadoPaginado<Colaborador>> ListarAsync(
        ColaboradorFiltro filtro, CancellationToken ct = default)
    {
        var consulta = _db.Colaboradores
            .AsNoTracking()
            .Include(c => c.Departamento)
            .Include(c => c.Localizacao)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filtro.Busca))
        {
            var termo = filtro.Busca.Trim();
            consulta = consulta.Where(c =>
                EF.Functions.Like(c.NomeCompleto, $"%{termo}%") ||
                EF.Functions.Like(c.Matricula, $"%{termo}%") ||
                EF.Functions.Like(c.Email, $"%{termo}%") ||
                (c.Cargo != null && EF.Functions.Like(c.Cargo, $"%{termo}%")) ||
                (c.Cpf != null && EF.Functions.Like(c.Cpf, $"%{termo}%")));
        }

        if (filtro.DepartamentoId.HasValue)
            consulta = consulta.Where(c => c.DepartamentoId == filtro.DepartamentoId);

        if (filtro.Status.HasValue)
            consulta = consulta.Where(c => c.Status == filtro.Status);

        if (filtro.ComAtivos)
            consulta = consulta.Where(c => _db.Ativos.Any(a => a.ColaboradorAtualId == c.Id));

        consulta = filtro.Ordenacao switch
        {
            "matricula" => consulta.OrderBy(c => c.Matricula),
            "departamento" => consulta.OrderBy(c => c.Departamento!.Nome).ThenBy(c => c.NomeCompleto),
            "recentes" => consulta.OrderByDescending(c => c.Id),
            _ => consulta.OrderBy(c => c.NomeCompleto)
        };

        return await consulta.PaginarAsync(filtro.Pagina, filtro.TamanhoPagina, ct);
    }

    public Task<Colaborador?> ObterAsync(int id, CancellationToken ct = default) =>
        _db.Colaboradores.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Colaborador?> ObterCompletoAsync(int id, CancellationToken ct = default) =>
        _db.Colaboradores
            .AsNoTracking()
            .Include(c => c.Departamento)
            .Include(c => c.Localizacao)
            .Include(c => c.Movimentacoes.OrderByDescending(m => m.DataRetirada))
                .ThenInclude(m => m.Ativo)
            .Include(c => c.DemandasSolicitadas.OrderByDescending(d => d.DataAbertura))
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<int> CriarAsync(Colaborador colaborador, CancellationToken ct = default)
    {
        await GarantirDadosUnicosAsync(colaborador, ct);

        colaborador.Matricula = colaborador.Matricula.Trim();
        colaborador.Email = colaborador.Email.Trim().ToLowerInvariant();
        colaborador.MarcarCriacao(_usuario.NomeExibicao);

        _db.Colaboradores.Add(colaborador);

        await _auditoria.RegistrarAsync(
            TipoAcaoAuditoria.Criacao, nameof(Colaborador), null,
            $"Colaborador {colaborador.NomeCompleto} (matrícula {colaborador.Matricula}) cadastrado.", ct);

        await _db.SaveChangesAsync(ct);
        return colaborador.Id;
    }

    public async Task AtualizarAsync(Colaborador colaborador, CancellationToken ct = default)
    {
        var existente = await _db.Colaboradores.FirstOrDefaultAsync(c => c.Id == colaborador.Id, ct)
            ?? throw new RegraDeNegocioException("Colaborador não encontrado.");

        await GarantirDadosUnicosAsync(colaborador, ct);

        if (colaborador.Status == StatusColaborador.Desligado)
        {
            var emPosse = await _db.Ativos.CountAsync(a => a.ColaboradorAtualId == colaborador.Id, ct);
            if (emPosse > 0)
                throw new RegraDeNegocioException(
                    $"Não é possível desligar: o colaborador ainda possui {emPosse} ativo(s) em posse. " +
                    "Registre as devoluções primeiro.");
        }

        var criadoEm = existente.CriadoEm;
        var criadoPor = existente.CriadoPor;
        var excluido = existente.Excluido;

        _db.Colaboradores.Entry(existente).CurrentValues.SetValues(colaborador);

        existente.CriadoEm = criadoEm;
        existente.CriadoPor = criadoPor;
        existente.Excluido = excluido;
        existente.Email = colaborador.Email.Trim().ToLowerInvariant();
        existente.MarcarAtualizacao(_usuario.NomeExibicao);

        await _auditoria.RegistrarAsync(
            TipoAcaoAuditoria.Alteracao, nameof(Colaborador), colaborador.Id,
            $"Colaborador {existente.NomeCompleto} atualizado.", ct);

        await _db.SaveChangesAsync(ct);
    }

    public async Task ExcluirAsync(int id, CancellationToken ct = default)
    {
        var colaborador = await _db.Colaboradores.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new RegraDeNegocioException("Colaborador não encontrado.");

        var emPosse = await _db.Ativos.CountAsync(a => a.ColaboradorAtualId == id, ct);
        if (emPosse > 0)
            throw new RegraDeNegocioException(
                $"O colaborador está com {emPosse} ativo(s). Registre a devolução antes de excluí-lo.");

        colaborador.Excluido = true;
        colaborador.Status = StatusColaborador.Desligado;
        colaborador.MarcarAtualizacao(_usuario.NomeExibicao);

        await _auditoria.RegistrarAsync(
            TipoAcaoAuditoria.Exclusao, nameof(Colaborador), id,
            $"Colaborador {colaborador.NomeCompleto} excluído.", ct);

        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Ativo>> ObterAtivosEmPosseAsync(
        int colaboradorId, CancellationToken ct = default) =>
        await _db.Ativos
            .AsNoTracking()
            .Where(a => a.ColaboradorAtualId == colaboradorId)
            .OrderBy(a => a.Tipo)
            .ToListAsync(ct);

    public async Task<IReadOnlyDictionary<int, int>> ContarAtivosEmPosseAsync(
        IEnumerable<int> colaboradorIds, CancellationToken ct = default)
    {
        var ids = colaboradorIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, int>();

        var contagens = await _db.Ativos
            .AsNoTracking()
            .Where(a => a.ColaboradorAtualId != null && ids.Contains(a.ColaboradorAtualId.Value))
            .GroupBy(a => a.ColaboradorAtualId!.Value)
            .Select(g => new { ColaboradorId = g.Key, Total = g.Count() })
            .ToListAsync(ct);

        return ids.ToDictionary(
            id => id,
            id => contagens.FirstOrDefault(c => c.ColaboradorId == id)?.Total ?? 0);
    }

    public async Task<IReadOnlyList<Colaborador>> ListarParaSelecaoAsync(CancellationToken ct = default) =>
        await _db.Colaboradores
            .AsNoTracking()
            .Include(c => c.Departamento)
            .Where(c => c.Status == StatusColaborador.Ativo || c.Status == StatusColaborador.Ferias)
            .OrderBy(c => c.NomeCompleto)
            .ToListAsync(ct);

    public Task<bool> MatriculaEmUsoAsync(string matricula, int? ignorarId = null, CancellationToken ct = default)
    {
        var valor = matricula.Trim();
        return _db.Colaboradores
            .IgnoreQueryFilters()
            .AnyAsync(c => c.Matricula == valor && (ignorarId == null || c.Id != ignorarId), ct);
    }

    public Task<bool> EmailEmUsoAsync(string email, int? ignorarId = null, CancellationToken ct = default)
    {
        var valor = email.Trim().ToLowerInvariant();
        return _db.Colaboradores
            .IgnoreQueryFilters()
            .AnyAsync(c => c.Email == valor && (ignorarId == null || c.Id != ignorarId), ct);
    }

    private async Task GarantirDadosUnicosAsync(Colaborador colaborador, CancellationToken ct)
    {
        var id = colaborador.Id == 0 ? (int?)null : colaborador.Id;

        if (await MatriculaEmUsoAsync(colaborador.Matricula, id, ct))
            throw new RegraDeNegocioException(
                $"A matrícula {colaborador.Matricula.Trim()} já está cadastrada.");

        if (await EmailEmUsoAsync(colaborador.Email, id, ct))
            throw new RegraDeNegocioException(
                $"O e-mail {colaborador.Email.Trim()} já está cadastrado para outro colaborador.");
    }
}
