using Elcop.TI.Application.Common;
using Elcop.TI.Application.Models;
using Elcop.TI.Domain.Entities;
using Elcop.TI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Elcop.TI.Application.Services;

/// <inheritdoc />
public class AuditoriaService : IAuditoriaService
{
    private readonly IAppDbContext _db;
    private readonly IUsuarioAtual _usuario;

    public AuditoriaService(IAppDbContext db, IUsuarioAtual usuario)
    {
        _db = db;
        _usuario = usuario;
    }

    public Task RegistrarAsync(
        TipoAcaoAuditoria acao,
        string entidade,
        int? entidadeId,
        string descricao,
        CancellationToken ct = default)
    {
        _db.RegistrosAuditoria.Add(new RegistroAuditoria
        {
            Acao = acao,
            Entidade = entidade,
            EntidadeId = entidadeId,
            Descricao = descricao.Length > 1000 ? descricao[..1000] : descricao,
            Usuario = _usuario.NomeExibicao ?? _usuario.NomeUsuario ?? "sistema",
            EnderecoIp = _usuario.EnderecoIp,
            DataHora = DateTime.Now
        });

        return Task.CompletedTask;
    }

    public async Task RegistrarESalvarAsync(
        TipoAcaoAuditoria acao,
        string entidade,
        int? entidadeId,
        string descricao,
        CancellationToken ct = default)
    {
        await RegistrarAsync(acao, entidade, entidadeId, descricao, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<ResultadoPaginado<RegistroAuditoria>> ListarAsync(
        string? busca,
        TipoAcaoAuditoria? acao,
        DateTime? de,
        DateTime? ate,
        int pagina,
        int tamanhoPagina,
        CancellationToken ct = default)
    {
        var consulta = _db.RegistrosAuditoria.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim();
            consulta = consulta.Where(r =>
                EF.Functions.Like(r.Descricao, $"%{termo}%") ||
                EF.Functions.Like(r.Usuario, $"%{termo}%") ||
                EF.Functions.Like(r.Entidade, $"%{termo}%"));
        }

        if (acao.HasValue)
            consulta = consulta.Where(r => r.Acao == acao.Value);

        if (de.HasValue)
            consulta = consulta.Where(r => r.DataHora >= de.Value.Date);

        if (ate.HasValue)
            consulta = consulta.Where(r => r.DataHora < ate.Value.Date.AddDays(1));

        return await consulta
            .OrderByDescending(r => r.DataHora)
            .PaginarAsync(pagina, tamanhoPagina, ct);
    }
}
