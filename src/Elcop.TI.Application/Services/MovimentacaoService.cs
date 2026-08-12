using Elcop.TI.Application.Common;
using Elcop.TI.Application.Models;
using Elcop.TI.Domain.Common;
using Elcop.TI.Domain.Entities;
using Elcop.TI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Elcop.TI.Application.Services;

/// <summary>
/// Orquestra o ciclo retirada → posse → devolução, mantendo o ativo e o termo
/// sempre consistentes entre si.
/// </summary>
public class MovimentacaoService : IMovimentacaoService
{
    private readonly IAppDbContext _db;
    private readonly IUsuarioAtual _usuario;
    private readonly IAuditoriaService _auditoria;

    public MovimentacaoService(IAppDbContext db, IUsuarioAtual usuario, IAuditoriaService auditoria)
    {
        _db = db;
        _usuario = usuario;
        _auditoria = auditoria;
    }

    public async Task<ResultadoPaginado<Movimentacao>> ListarAsync(
        MovimentacaoFiltro filtro, CancellationToken ct = default)
    {
        var consulta = _db.Movimentacoes
            .AsNoTracking()
            .Include(m => m.Ativo)
            .Include(m => m.Colaborador).ThenInclude(c => c!.Departamento)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filtro.Busca))
        {
            var termo = filtro.Busca.Trim();
            consulta = consulta.Where(m =>
                EF.Functions.Like(m.Protocolo, $"%{termo}%") ||
                (m.Ativo != null && EF.Functions.Like(m.Ativo.Patrimonio, $"%{termo}%")) ||
                (m.Ativo != null && EF.Functions.Like(m.Ativo.Modelo, $"%{termo}%")) ||
                (m.Ativo != null && m.Ativo.NumeroSerie != null
                    && EF.Functions.Like(m.Ativo.NumeroSerie, $"%{termo}%")) ||
                (m.Colaborador != null && EF.Functions.Like(m.Colaborador.NomeCompleto, $"%{termo}%")) ||
                (m.Colaborador != null && EF.Functions.Like(m.Colaborador.Matricula, $"%{termo}%")));
        }

        if (filtro.Tipo.HasValue) consulta = consulta.Where(m => m.Tipo == filtro.Tipo);
        if (filtro.Status.HasValue) consulta = consulta.Where(m => m.Status == filtro.Status);
        if (filtro.ColaboradorId.HasValue) consulta = consulta.Where(m => m.ColaboradorId == filtro.ColaboradorId);
        if (filtro.AtivoId.HasValue) consulta = consulta.Where(m => m.AtivoId == filtro.AtivoId);
        if (filtro.DataInicial.HasValue) consulta = consulta.Where(m => m.DataRetirada >= filtro.DataInicial.Value.Date);
        if (filtro.DataFinal.HasValue) consulta = consulta.Where(m => m.DataRetirada < filtro.DataFinal.Value.Date.AddDays(1));

        if (filtro.SomenteAtrasadas)
        {
            consulta = consulta.Where(m =>
                (m.Status == StatusMovimentacao.EmAberto || m.Status == StatusMovimentacao.Atrasado)
                && m.DataPrevistaDevolucao != null
                && m.DataPrevistaDevolucao < DateTime.Today);
        }

        consulta = filtro.Ordenacao switch
        {
            "colaborador" => consulta.OrderBy(m => m.Colaborador!.NomeCompleto),
            "ativo" => consulta.OrderBy(m => m.Ativo!.Patrimonio),
            "previsao" => consulta.OrderBy(m => m.DataPrevistaDevolucao ?? DateTime.MaxValue),
            "antigas" => consulta.OrderBy(m => m.DataRetirada),
            _ => consulta.OrderByDescending(m => m.DataRetirada).ThenByDescending(m => m.Id)
        };

        return await consulta.PaginarAsync(filtro.Pagina, filtro.TamanhoPagina, ct);
    }

    public Task<Movimentacao?> ObterAsync(int id, CancellationToken ct = default) =>
        _db.Movimentacoes
            .AsNoTracking()
            .Include(m => m.Ativo).ThenInclude(a => a!.Fornecedor)
            .Include(m => m.Colaborador).ThenInclude(c => c!.Departamento)
            .Include(m => m.Colaborador).ThenInclude(c => c!.Localizacao)
            .FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<int> RegistrarEntregaAsync(EntregaAtivoModel model, CancellationToken ct = default)
    {
        var ativo = await _db.Ativos.FirstOrDefaultAsync(a => a.Id == model.AtivoId, ct)
            ?? throw new RegraDeNegocioException("Ativo não encontrado.");

        var colaborador = await _db.Colaboradores
            .Include(c => c.Departamento)
            .FirstOrDefaultAsync(c => c.Id == model.ColaboradorId, ct)
            ?? throw new RegraDeNegocioException("Colaborador não encontrado.");

        if (ativo.ColaboradorAtualId.HasValue)
        {
            var atual = await _db.Colaboradores
                .Where(c => c.Id == ativo.ColaboradorAtualId)
                .Select(c => c.NomeCompleto)
                .FirstOrDefaultAsync(ct);

            throw new RegraDeNegocioException(
                $"O ativo {ativo.Patrimonio} já está em posse de {atual ?? "outro colaborador"}. " +
                "Registre a devolução ou use a transferência.");
        }

        if (ativo.Status is StatusAtivo.Baixado or StatusAtivo.Extraviado)
            throw new RegraDeNegocioException(
                $"O ativo {ativo.Patrimonio} está com status \"{ativo.Status.ObterNome()}\" e não pode ser entregue.");

        if (ativo.Status == StatusAtivo.EmManutencao && model.Tipo != TipoMovimentacao.Manutencao)
            throw new RegraDeNegocioException(
                $"O ativo {ativo.Patrimonio} está em manutenção. Finalize a manutenção antes de entregá-lo.");

        if (!colaborador.PodeReceberAtivos)
            throw new RegraDeNegocioException(
                $"{colaborador.NomeCompleto} está com a situação \"{colaborador.Status.ObterNome()}\" " +
                "e não pode receber ativos.");

        var movimentacao = new Movimentacao
        {
            Protocolo = await GerarProtocoloAsync(ct),
            AtivoId = ativo.Id,
            ColaboradorId = colaborador.Id,
            Tipo = model.Tipo,
            Status = StatusMovimentacao.EmAberto,
            DataRetirada = model.DataRetirada,
            DataPrevistaDevolucao = model.DataPrevistaDevolucao,
            CondicaoRetirada = model.CondicaoRetirada,
            AcessoriosEntregues = string.IsNullOrWhiteSpace(model.AcessoriosEntregues)
                ? ativo.Acessorios
                : model.AcessoriosEntregues,
            ResponsavelEntrega = model.ResponsavelEntrega ?? _usuario.NomeExibicao,
            LocalEntrega = model.LocalEntrega,
            ObservacoesRetirada = model.ObservacoesRetirada,
            TermoAssinado = model.TermoAssinado
        };
        movimentacao.MarcarCriacao(_usuario.NomeExibicao);

        _db.Movimentacoes.Add(movimentacao);

        ativo.ColaboradorAtualId = colaborador.Id;
        ativo.Status = StatusParaTipo(model.Tipo);
        ativo.Condicao = model.CondicaoRetirada;
        ativo.MarcarAtualizacao(_usuario.NomeExibicao);

        if (model.HerdarLotacao)
        {
            if (colaborador.DepartamentoId.HasValue) ativo.DepartamentoId = colaborador.DepartamentoId;
            if (colaborador.LocalizacaoId.HasValue) ativo.LocalizacaoId = colaborador.LocalizacaoId;
        }

        await _auditoria.RegistrarAsync(
            TipoAcaoAuditoria.Movimentacao, nameof(Movimentacao), null,
            $"{model.Tipo.ObterNome()}: ativo {ativo.Patrimonio} ({ativo.DescricaoCurta}) " +
            $"entregue a {colaborador.NomeCompleto}.", ct);

        await _db.SaveChangesAsync(ct);
        return movimentacao.Id;
    }

    public async Task RegistrarDevolucaoAsync(DevolucaoAtivoModel model, CancellationToken ct = default)
    {
        var movimentacao = await _db.Movimentacoes
            .Include(m => m.Ativo)
            .Include(m => m.Colaborador)
            .FirstOrDefaultAsync(m => m.Id == model.MovimentacaoId, ct)
            ?? throw new RegraDeNegocioException("Movimentação não encontrada.");

        if (!movimentacao.EstaEmAberto)
            throw new RegraDeNegocioException(
                $"O termo {movimentacao.Protocolo} já está com a situação \"{movimentacao.Status.ObterNome()}\".");

        if (model.DataDevolucao < movimentacao.DataRetirada)
            throw new RegraDeNegocioException("A devolução não pode ser anterior à data de retirada.");

        movimentacao.RegistrarDevolucao(
            model.DataDevolucao,
            model.CondicaoDevolucao,
            model.ResponsavelRecebimento ?? _usuario.NomeExibicao,
            model.AcessoriosDevolvidos,
            model.ObservacoesDevolucao,
            model.ComAvaria);
        movimentacao.MarcarAtualizacao(_usuario.NomeExibicao);

        if (movimentacao.Ativo is { } ativo)
        {
            ativo.ColaboradorAtualId = null;
            ativo.Condicao = model.CondicaoDevolucao;
            ativo.Status = model.ComAvaria && model.StatusDestino == StatusAtivo.Disponivel
                ? StatusAtivo.EmManutencao
                : model.StatusDestino;
            ativo.MarcarAtualizacao(_usuario.NomeExibicao);
        }

        await _auditoria.RegistrarAsync(
            TipoAcaoAuditoria.Movimentacao, nameof(Movimentacao), movimentacao.Id,
            $"Devolução do ativo {movimentacao.Ativo?.Patrimonio} por " +
            $"{movimentacao.Colaborador?.NomeCompleto} (termo {movimentacao.Protocolo})" +
            (model.ComAvaria ? " — devolvido com avaria." : "."), ct);

        await _db.SaveChangesAsync(ct);
    }

    public async Task<int> TransferirAsync(
        int movimentacaoId, int novoColaboradorId, string? observacoes, CancellationToken ct = default)
    {
        var origem = await _db.Movimentacoes
            .Include(m => m.Ativo)
            .FirstOrDefaultAsync(m => m.Id == movimentacaoId, ct)
            ?? throw new RegraDeNegocioException("Movimentação não encontrada.");

        if (!origem.EstaEmAberto)
            throw new RegraDeNegocioException("Só é possível transferir um ativo que esteja em posse.");

        if (origem.ColaboradorId == novoColaboradorId)
            throw new RegraDeNegocioException("O ativo já está com este colaborador.");

        // Devolução + nova entrega precisam valer como uma única operação: se a segunda
        // etapa falhar, o ativo não pode ficar "solto" no estoque.
        await using var transacao = await _db.Database.BeginTransactionAsync(ct);

        await RegistrarDevolucaoAsync(new DevolucaoAtivoModel
        {
            MovimentacaoId = origem.Id,
            DataDevolucao = DateTime.Now,
            CondicaoDevolucao = origem.Ativo?.Condicao ?? CondicaoAtivo.Bom,
            ResponsavelRecebimento = _usuario.NomeExibicao,
            ObservacoesDevolucao = $"Transferência de posse. {observacoes}".Trim(),
            StatusDestino = StatusAtivo.Disponivel,
            DataRetiradaOriginal = origem.DataRetirada
        }, ct);

        var novaMovimentacaoId = await RegistrarEntregaAsync(new EntregaAtivoModel
        {
            AtivoId = origem.AtivoId,
            ColaboradorId = novoColaboradorId,
            Tipo = TipoMovimentacao.Transferencia,
            DataRetirada = DateTime.Now,
            CondicaoRetirada = origem.Ativo?.Condicao ?? CondicaoAtivo.Bom,
            AcessoriosEntregues = origem.AcessoriosEntregues,
            ResponsavelEntrega = _usuario.NomeExibicao,
            ObservacoesRetirada = $"Transferido do termo {origem.Protocolo}. {observacoes}".Trim()
        }, ct);

        await transacao.CommitAsync(ct);
        return novaMovimentacaoId;
    }

    public async Task CancelarAsync(int id, string motivo, CancellationToken ct = default)
    {
        var movimentacao = await _db.Movimentacoes
            .Include(m => m.Ativo)
            .FirstOrDefaultAsync(m => m.Id == id, ct)
            ?? throw new RegraDeNegocioException("Movimentação não encontrada.");

        if (movimentacao.Status == StatusMovimentacao.Devolvido)
            throw new RegraDeNegocioException("Movimentações já devolvidas não podem ser canceladas.");

        movimentacao.Status = StatusMovimentacao.Cancelado;
        movimentacao.ObservacoesDevolucao =
            $"Cancelado em {DateTime.Now:dd/MM/yyyy HH:mm} por {_usuario.NomeExibicao}. Motivo: {motivo}";
        movimentacao.MarcarAtualizacao(_usuario.NomeExibicao);

        if (movimentacao.Ativo is { } ativo && ativo.ColaboradorAtualId == movimentacao.ColaboradorId)
        {
            ativo.ColaboradorAtualId = null;
            ativo.Status = StatusAtivo.Disponivel;
            ativo.MarcarAtualizacao(_usuario.NomeExibicao);
        }

        await _auditoria.RegistrarAsync(
            TipoAcaoAuditoria.Movimentacao, nameof(Movimentacao), id,
            $"Termo {movimentacao.Protocolo} cancelado. Motivo: {motivo}", ct);

        await _db.SaveChangesAsync(ct);
    }

    public Task<Movimentacao?> ObterEmAbertoDoAtivoAsync(int ativoId, CancellationToken ct = default) =>
        _db.Movimentacoes
            .Include(m => m.Colaborador)
            .Include(m => m.Ativo)
            .Where(m => m.AtivoId == ativoId
                     && (m.Status == StatusMovimentacao.EmAberto || m.Status == StatusMovimentacao.Atrasado))
            .OrderByDescending(m => m.DataRetirada)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<Movimentacao>> ListarEmAbertoAsync(CancellationToken ct = default) =>
        await _db.Movimentacoes
            .AsNoTracking()
            .Include(m => m.Ativo)
            .Include(m => m.Colaborador)
            .Where(m => m.Status == StatusMovimentacao.EmAberto || m.Status == StatusMovimentacao.Atrasado)
            .OrderBy(m => m.DataPrevistaDevolucao ?? DateTime.MaxValue)
            .ToListAsync(ct);

    public async Task<int> AtualizarAtrasosAsync(CancellationToken ct = default)
    {
        var vencidas = await _db.Movimentacoes
            .Where(m => m.Status == StatusMovimentacao.EmAberto
                     && m.DataPrevistaDevolucao != null
                     && m.DataPrevistaDevolucao < DateTime.Today)
            .ToListAsync(ct);

        foreach (var movimentacao in vencidas)
            movimentacao.Status = StatusMovimentacao.Atrasado;

        if (vencidas.Count > 0)
            await _db.SaveChangesAsync(ct);

        return vencidas.Count;
    }

    public async Task<DevolucaoAtivoModel?> PrepararDevolucaoAsync(
        int movimentacaoId, CancellationToken ct = default)
    {
        var movimentacao = await _db.Movimentacoes
            .AsNoTracking()
            .Include(m => m.Ativo)
            .FirstOrDefaultAsync(m => m.Id == movimentacaoId, ct);

        if (movimentacao is null) return null;

        return new DevolucaoAtivoModel
        {
            MovimentacaoId = movimentacao.Id,
            DataDevolucao = DateTime.Now,
            CondicaoDevolucao = movimentacao.Ativo?.Condicao ?? movimentacao.CondicaoRetirada,
            AcessoriosDevolvidos = movimentacao.AcessoriosEntregues,
            ResponsavelRecebimento = _usuario.NomeExibicao,
            StatusDestino = StatusAtivo.Disponivel,
            DataRetiradaOriginal = movimentacao.DataRetirada
        };
    }

    /// <summary>Status que o ativo assume conforme a natureza do movimento.</summary>
    private static StatusAtivo StatusParaTipo(TipoMovimentacao tipo) => tipo switch
    {
        TipoMovimentacao.Emprestimo => StatusAtivo.Emprestado,
        TipoMovimentacao.Manutencao => StatusAtivo.EmManutencao,
        TipoMovimentacao.Baixa => StatusAtivo.Baixado,
        _ => StatusAtivo.EmUso
    };

    private async Task<string> GerarProtocoloAsync(CancellationToken ct)
    {
        var ano = DateTime.Now.Year;
        var prefixo = $"MOV-{ano}-";

        var sequencia = await _db.Movimentacoes
            .Where(m => m.Protocolo.StartsWith(prefixo))
            .CountAsync(ct);

        // Protege contra colisão caso algum termo tenha sido removido da sequência.
        string protocolo;
        do
        {
            sequencia++;
            protocolo = $"{prefixo}{sequencia:D5}";
        }
        while (await _db.Movimentacoes.AnyAsync(m => m.Protocolo == protocolo, ct));

        return protocolo;
    }
}
