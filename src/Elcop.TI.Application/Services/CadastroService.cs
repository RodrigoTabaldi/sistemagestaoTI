using Elcop.TI.Application.Common;
using Elcop.TI.Domain.Common;
using Elcop.TI.Domain.Entities;
using Elcop.TI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Elcop.TI.Application.Services;

/// <inheritdoc />
public class CadastroService : ICadastroService
{
    private readonly IAppDbContext _db;
    private readonly IUsuarioAtual _usuario;
    private readonly IAuditoriaService _auditoria;
    private readonly ICacheService _cache;

    public CadastroService(IAppDbContext db, IUsuarioAtual usuario, IAuditoriaService auditoria, ICacheService cache)
    {
        _db = db;
        _usuario = usuario;
        _auditoria = auditoria;
        _cache = cache;
    }

    // ---------------------------------------------------------------- Departamentos

    public async Task<IReadOnlyList<Departamento>> ListarDepartamentosAsync(
        bool somenteHabilitados = false, CancellationToken ct = default)
    {
        var chave = $"departamentos_{somenteHabilitados}";
        return await _cache.ObterOuCriarAsync(
            chave,
            TimeSpan.FromMinutes(5),
            async () =>
            {
                var lista = await _db.Departamentos
                    .AsNoTracking()
                    .Where(d => !somenteHabilitados || d.Habilitado)
                    .OrderBy(d => d.Nome)
                    .ToListAsync(ct);
                return (IReadOnlyList<Departamento>)lista;
            },
            ct);
    }

    public Task<Departamento?> ObterDepartamentoAsync(int id, CancellationToken ct = default) =>
        _db.Departamentos.FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task SalvarDepartamentoAsync(Departamento departamento, CancellationToken ct = default)
    {
        var novoNome = departamento.Nome.Trim();
        var existentes = await _db.Departamentos
            .AsNoTracking()
            .Select(d => new { d.Id, d.Nome })
            .ToListAsync(ct);

        var duplicado = existentes.Any(d =>
            string.Equals(d.Nome, novoNome, StringComparison.OrdinalIgnoreCase) && d.Id != departamento.Id);

        if (duplicado)
            throw new RegraDeNegocioException($"Já existe o departamento \"{novoNome}\".");

        if (departamento.Id == 0)
        {
            departamento.MarcarCriacao(_usuario.NomeExibicao);
            _db.Departamentos.Add(departamento);
        }
        else
        {
            var existente = await ObterDepartamentoAsync(departamento.Id, ct)
                ?? throw new RegraDeNegocioException("Departamento não encontrado.");

            existente.Nome = departamento.Nome.Trim();
            existente.Sigla = departamento.Sigla;
            existente.CentroCusto = departamento.CentroCusto;
            existente.Responsavel = departamento.Responsavel;
            existente.Habilitado = departamento.Habilitado;
            existente.MarcarAtualizacao(_usuario.NomeExibicao);
        }

        await _auditoria.RegistrarAsync(
            departamento.Id == 0 ? TipoAcaoAuditoria.Criacao : TipoAcaoAuditoria.Alteracao,
            nameof(Departamento), departamento.Id == 0 ? null : departamento.Id,
            $"Departamento \"{departamento.Nome.Trim()}\" salvo.", ct);

        await _db.SaveChangesAsync(ct);

        _cache.Remover("departamentos_false");
        _cache.Remover("departamentos_true");
    }

    public async Task ExcluirDepartamentoAsync(int id, CancellationToken ct = default)
    {
        var departamento = await ObterDepartamentoAsync(id, ct)
            ?? throw new RegraDeNegocioException("Departamento não encontrado.");

        var vinculos = await _db.Colaboradores.CountAsync(c => c.DepartamentoId == id, ct)
                     + await _db.Ativos.CountAsync(a => a.DepartamentoId == id, ct);

        if (vinculos > 0)
            throw new RegraDeNegocioException(
                $"O departamento possui {vinculos} vínculo(s) ativo(s). Desative-o em vez de excluir.");

        departamento.Excluido = true;
        departamento.Habilitado = false;

        await _auditoria.RegistrarAsync(
            TipoAcaoAuditoria.Exclusao, nameof(Departamento), id,
            $"Departamento \"{departamento.Nome}\" excluído.", ct);

        await _db.SaveChangesAsync(ct);

        _cache.Remover("departamentos_false");
        _cache.Remover("departamentos_true");
    }

    // ---------------------------------------------------------------- Localizações

    public async Task<IReadOnlyList<Localizacao>> ListarLocalizacoesAsync(
        bool somenteHabilitadas = false, CancellationToken ct = default)
    {
        var chave = $"localizacoes_{somenteHabilitadas}";
        return await _cache.ObterOuCriarAsync(
            chave,
            TimeSpan.FromMinutes(5),
            async () =>
            {
                var lista = await _db.Localizacoes
                    .AsNoTracking()
                    .Where(l => !somenteHabilitadas || l.Habilitado)
                    .OrderBy(l => l.Unidade).ThenBy(l => l.Nome)
                    .ToListAsync(ct);
                return (IReadOnlyList<Localizacao>)lista;
            },
            ct);
    }

    public Task<Localizacao?> ObterLocalizacaoAsync(int id, CancellationToken ct = default) =>
        _db.Localizacoes.FirstOrDefaultAsync(l => l.Id == id, ct);

    public async Task SalvarLocalizacaoAsync(Localizacao localizacao, CancellationToken ct = default)
    {
        if (localizacao.Id == 0)
        {
            localizacao.MarcarCriacao(_usuario.NomeExibicao);
            _db.Localizacoes.Add(localizacao);
        }
        else
        {
            var existente = await ObterLocalizacaoAsync(localizacao.Id, ct)
                ?? throw new RegraDeNegocioException("Localização não encontrada.");

            existente.Nome = localizacao.Nome.Trim();
            existente.Unidade = localizacao.Unidade;
            existente.Endereco = localizacao.Endereco;
            existente.Cidade = localizacao.Cidade;
            existente.Uf = localizacao.Uf?.ToUpperInvariant();
            existente.Habilitado = localizacao.Habilitado;
            existente.MarcarAtualizacao(_usuario.NomeExibicao);
        }

        await _auditoria.RegistrarAsync(
            localizacao.Id == 0 ? TipoAcaoAuditoria.Criacao : TipoAcaoAuditoria.Alteracao,
            nameof(Localizacao), localizacao.Id == 0 ? null : localizacao.Id,
            $"Localização \"{localizacao.Nome.Trim()}\" salva.", ct);

        await _db.SaveChangesAsync(ct);

        _cache.Remover("localizacoes_false");
        _cache.Remover("localizacoes_true");
    }

    public async Task ExcluirLocalizacaoAsync(int id, CancellationToken ct = default)
    {
        var localizacao = await ObterLocalizacaoAsync(id, ct)
            ?? throw new RegraDeNegocioException("Localização não encontrada.");

        var vinculos = await _db.Ativos.CountAsync(a => a.LocalizacaoId == id, ct)
                     + await _db.Colaboradores.CountAsync(c => c.LocalizacaoId == id, ct);

        if (vinculos > 0)
            throw new RegraDeNegocioException(
                $"A localização possui {vinculos} vínculo(s). Desative-a em vez de excluir.");

        localizacao.Excluido = true;
        localizacao.Habilitado = false;

        await _auditoria.RegistrarAsync(
            TipoAcaoAuditoria.Exclusao, nameof(Localizacao), id,
            $"Localização \"{localizacao.Nome}\" excluída.", ct);

        await _db.SaveChangesAsync(ct);

        _cache.Remover("localizacoes_false");
        _cache.Remover("localizacoes_true");
    }

    // ---------------------------------------------------------------- Fornecedores

    public async Task<IReadOnlyList<Fornecedor>> ListarFornecedoresAsync(
        bool somenteHabilitados = false, CancellationToken ct = default)
    {
        var chave = $"fornecedores_{somenteHabilitados}";
        return await _cache.ObterOuCriarAsync(
            chave,
            TimeSpan.FromMinutes(5),
            async () =>
            {
                var lista = await _db.Fornecedores
                    .AsNoTracking()
                    .Where(f => !somenteHabilitados || f.Habilitado)
                    .OrderBy(f => f.Nome)
                    .ToListAsync(ct);
                return (IReadOnlyList<Fornecedor>)lista;
            },
            ct);
    }

    public Task<Fornecedor?> ObterFornecedorAsync(int id, CancellationToken ct = default) =>
        _db.Fornecedores.FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task SalvarFornecedorAsync(Fornecedor fornecedor, CancellationToken ct = default)
    {
        if (fornecedor.Id == 0)
        {
            fornecedor.MarcarCriacao(_usuario.NomeExibicao);
            _db.Fornecedores.Add(fornecedor);
        }
        else
        {
            var existente = await ObterFornecedorAsync(fornecedor.Id, ct)
                ?? throw new RegraDeNegocioException("Fornecedor não encontrado.");

            existente.Nome = fornecedor.Nome.Trim();
            existente.Cnpj = fornecedor.Cnpj;
            existente.Contato = fornecedor.Contato;
            existente.Telefone = fornecedor.Telefone;
            existente.Email = fornecedor.Email;
            existente.Observacoes = fornecedor.Observacoes;
            existente.Habilitado = fornecedor.Habilitado;
            existente.MarcarAtualizacao(_usuario.NomeExibicao);
        }

        await _auditoria.RegistrarAsync(
            fornecedor.Id == 0 ? TipoAcaoAuditoria.Criacao : TipoAcaoAuditoria.Alteracao,
            nameof(Fornecedor), fornecedor.Id == 0 ? null : fornecedor.Id,
            $"Fornecedor \"{fornecedor.Nome.Trim()}\" salvo.", ct);

        await _db.SaveChangesAsync(ct);

        _cache.Remover("fornecedores_false");
        _cache.Remover("fornecedores_true");
    }

    public async Task ExcluirFornecedorAsync(int id, CancellationToken ct = default)
    {
        var fornecedor = await ObterFornecedorAsync(id, ct)
            ?? throw new RegraDeNegocioException("Fornecedor não encontrado.");

        var vinculos = await _db.Ativos.CountAsync(a => a.FornecedorId == id, ct);
        if (vinculos > 0)
            throw new RegraDeNegocioException(
                $"O fornecedor está vinculado a {vinculos} ativo(s). Desative-o em vez de excluir.");

        fornecedor.Excluido = true;
        fornecedor.Habilitado = false;

        await _auditoria.RegistrarAsync(
            TipoAcaoAuditoria.Exclusao, nameof(Fornecedor), id,
            $"Fornecedor \"{fornecedor.Nome}\" excluído.", ct);

        await _db.SaveChangesAsync(ct);

        _cache.Remover("fornecedores_false");
        _cache.Remover("fornecedores_true");
    }
}
