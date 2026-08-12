using Elcop.TI.Domain.Entities;

namespace Elcop.TI.Application.Services;

/// <summary>
/// Cadastros de apoio (departamentos, localizações e fornecedores) usados
/// como listas de seleção em todo o sistema.
/// </summary>
public interface ICadastroService
{
    Task<IReadOnlyList<Departamento>> ListarDepartamentosAsync(bool somenteHabilitados = false, CancellationToken ct = default);
    Task<Departamento?> ObterDepartamentoAsync(int id, CancellationToken ct = default);
    Task SalvarDepartamentoAsync(Departamento departamento, CancellationToken ct = default);
    Task ExcluirDepartamentoAsync(int id, CancellationToken ct = default);

    Task<IReadOnlyList<Localizacao>> ListarLocalizacoesAsync(bool somenteHabilitadas = false, CancellationToken ct = default);
    Task<Localizacao?> ObterLocalizacaoAsync(int id, CancellationToken ct = default);
    Task SalvarLocalizacaoAsync(Localizacao localizacao, CancellationToken ct = default);
    Task ExcluirLocalizacaoAsync(int id, CancellationToken ct = default);

    Task<IReadOnlyList<Fornecedor>> ListarFornecedoresAsync(bool somenteHabilitados = false, CancellationToken ct = default);
    Task<Fornecedor?> ObterFornecedorAsync(int id, CancellationToken ct = default);
    Task SalvarFornecedorAsync(Fornecedor fornecedor, CancellationToken ct = default);
    Task ExcluirFornecedorAsync(int id, CancellationToken ct = default);
}
