using System.ComponentModel.DataAnnotations;
using Elcop.TI.Application.Common;
using Elcop.TI.Application.Models;
using Elcop.TI.Domain.Entities;
using Elcop.TI.Domain.Enums;

namespace Elcop.TI.Web.Models;

// ------------------------------------------------------------------ Ativos

public class ListagemAtivosViewModel
{
    public ResultadoPaginado<Ativo> Pagina { get; init; } = ResultadoPaginado<Ativo>.Vazio();
    public AtivoFiltro Filtro { get; init; } = new();
    public ListasDeSelecao Listas { get; init; } = new();
    public IReadOnlyList<ResumoPorTipo> Resumo { get; init; } = Array.Empty<ResumoPorTipo>();
}

public class AtivoFormViewModel
{
    public Ativo Ativo { get; set; } = new();
    public ListasDeSelecao Listas { get; init; } = new();
    public bool Edicao => Ativo.Id > 0;
    public string Titulo => Edicao ? "Editar ativo" : "Novo ativo";
}

public class DetalhesAtivoViewModel
{
    public Ativo Ativo { get; init; } = new();
    public Movimentacao? PosseAtual { get; init; }
    public IReadOnlyList<Movimentacao> Historico { get; init; } = Array.Empty<Movimentacao>();
    public IReadOnlyList<Demanda> Demandas { get; init; } = Array.Empty<Demanda>();
}

// ------------------------------------------------------------------ Colaboradores

public class ListagemColaboradoresViewModel
{
    public ResultadoPaginado<Colaborador> Pagina { get; init; } = ResultadoPaginado<Colaborador>.Vazio();
    public ColaboradorFiltro Filtro { get; init; } = new();
    public ListasDeSelecao Listas { get; init; } = new();
    public IReadOnlyDictionary<int, int> AtivosPorColaborador { get; init; } = new Dictionary<int, int>();
}

public class ColaboradorFormViewModel
{
    public Colaborador Colaborador { get; set; } = new();
    public ListasDeSelecao Listas { get; init; } = new();
    public bool Edicao => Colaborador.Id > 0;
    public string Titulo => Edicao ? "Editar colaborador" : "Novo colaborador";
}

public class DetalhesColaboradorViewModel
{
    public Colaborador Colaborador { get; init; } = new();
    public IReadOnlyList<Ativo> AtivosEmPosse { get; init; } = Array.Empty<Ativo>();
    public IReadOnlyList<Movimentacao> Historico { get; init; } = Array.Empty<Movimentacao>();
    public IReadOnlyList<Demanda> Demandas { get; init; } = Array.Empty<Demanda>();
}

// ------------------------------------------------------------------ Movimentações

public class ListagemMovimentacoesViewModel
{
    public ResultadoPaginado<Movimentacao> Pagina { get; init; } = ResultadoPaginado<Movimentacao>.Vazio();
    public MovimentacaoFiltro Filtro { get; init; } = new();
    public ListasDeSelecao Listas { get; init; } = new();
    public int TotalEmAberto { get; init; }
    public int TotalAtrasadas { get; init; }
}

public class EntregaViewModel
{
    public EntregaAtivoModel Entrega { get; set; } = new();
    public ListasDeSelecao Listas { get; init; } = new();

    /// <summary>Catálogo enviado ao front para preencher o resumo do ativo escolhido.</summary>
    public IReadOnlyList<ResumoAtivoJson> CatalogoAtivos { get; init; } = Array.Empty<ResumoAtivoJson>();

    public IReadOnlyList<ResumoColaboradorJson> CatalogoColaboradores { get; init; } = Array.Empty<ResumoColaboradorJson>();
}

public record ResumoAtivoJson(
    int Id, string Patrimonio, string Tipo, string Descricao, string? Serie,
    string? Imei, string? Linha, string Condicao, string? Acessorios, string Icone);

public record ResumoColaboradorJson(
    int Id, string Nome, string Matricula, string Email, string? Cargo,
    string? Departamento, string Iniciais, int AtivosEmPosse);

public class DevolucaoViewModel
{
    public DevolucaoAtivoModel Devolucao { get; set; } = new();
    public Movimentacao Movimentacao { get; init; } = new();
}

public class TransferenciaViewModel
{
    public int MovimentacaoId { get; set; }

    [Required(ErrorMessage = "Selecione o colaborador de destino.")]
    [Display(Name = "Novo responsável")]
    public int NovoColaboradorId { get; set; }

    [Display(Name = "Observações da transferência")]
    [StringLength(1000)]
    public string? Observacoes { get; set; }

    public Movimentacao? Movimentacao { get; init; }
    public ListasDeSelecao Listas { get; init; } = new();
}

// ------------------------------------------------------------------ Demandas

public class ListagemDemandasViewModel
{
    public ResultadoPaginado<Demanda> Pagina { get; init; } = ResultadoPaginado<Demanda>.Vazio();
    public DemandaFiltro Filtro { get; init; } = new();
    public ListasDeSelecao Listas { get; init; } = new();
    public int TotalAbertas { get; init; }
    public int TotalAtrasadas { get; init; }
    public int TotalConcluidasMes { get; init; }
}

public class QuadroDemandasViewModel
{
    public QuadroKanban Quadro { get; init; } = new();
    public ListasDeSelecao Listas { get; init; } = new();
}

public class DemandaFormViewModel
{
    public Demanda Demanda { get; set; } = new();
    public ListasDeSelecao Listas { get; init; } = new();
    public bool Edicao => Demanda.Id > 0;
    public string Titulo => Edicao ? $"Editar demanda {Demanda.Codigo}" : "Nova demanda";
}

public class DetalhesDemandaViewModel
{
    public Demanda Demanda { get; init; } = new();
    public NovoAndamentoModel NovoAndamento { get; init; } = new();
}

// ------------------------------------------------------------------ Usuários

public class UsuarioViewModel
{
    public string? Id { get; set; }

    [Required(ErrorMessage = "Informe o nome completo.")]
    [Display(Name = "Nome completo")]
    [StringLength(160)]
    public string NomeCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    [Display(Name = "E-mail (login)")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Cargo")]
    [StringLength(120)]
    public string? Cargo { get; set; }

    [Required(ErrorMessage = "Selecione o perfil de acesso.")]
    [Display(Name = "Perfil de acesso")]
    public string Perfil { get; set; } = string.Empty;

    [Display(Name = "Usuário habilitado")]
    public bool Habilitado { get; set; } = true;

    [DataType(DataType.Password)]
    [Display(Name = "Senha")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "A senha deve ter ao menos 8 caracteres.")]
    public string? Senha { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Confirmar senha")]
    [Compare(nameof(Senha), ErrorMessage = "As senhas não conferem.")]
    public string? ConfirmarSenha { get; set; }

    public DateTime? UltimoAcesso { get; set; }

    public bool Edicao => !string.IsNullOrEmpty(Id);
}

// ------------------------------------------------------------------ Conta

public class LoginViewModel
{
    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha.")]
    [DataType(DataType.Password)]
    [Display(Name = "Senha")]
    public string Senha { get; set; } = string.Empty;

    [Display(Name = "Manter conectado")]
    public bool Lembrar { get; set; } = true;

    public string? UrlRetorno { get; set; }

    /// <summary>
    /// Preenchido pelo controller quando o login via Firebase está configurado.
    /// Nulo faz a tela renderizar apenas o formulário de e-mail e senha.
    /// </summary>
    public FirebaseLoginViewModel? Firebase { get; set; }
}

/// <summary>Dados públicos do projeto Firebase usados pelo SDK web na tela de login.</summary>
public class FirebaseLoginViewModel
{
    public string ApiKey { get; set; } = string.Empty;
    public string AuthDomain { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
}

/// <summary>Autocadastro pela tela de login.</summary>
public class RegistroViewModel
{
    [Required(ErrorMessage = "Informe seu nome completo.")]
    [StringLength(160, MinimumLength = 3, ErrorMessage = "O nome deve ter entre 3 e 160 caracteres.")]
    [Display(Name = "Nome completo")]
    public string NomeCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    [StringLength(200)]
    [Display(Name = "E-mail corporativo")]
    public string Email { get; set; } = string.Empty;

    [StringLength(120)]
    [Display(Name = "Cargo")]
    public string? Cargo { get; set; }

    [Required(ErrorMessage = "Informe a senha.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "A senha deve ter no mínimo 8 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Senha")]
    public string Senha { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme a senha.")]
    [Compare(nameof(Senha), ErrorMessage = "As senhas não conferem.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar senha")]
    public string ConfirmacaoSenha { get; set; } = string.Empty;
}

public class AlterarSenhaViewModel
{
    [Required(ErrorMessage = "Informe a senha atual.")]
    [DataType(DataType.Password)]
    [Display(Name = "Senha atual")]
    public string SenhaAtual { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a nova senha.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nova senha")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "A senha deve ter ao menos 8 caracteres.")]
    public string NovaSenha { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirmar nova senha")]
    [Compare(nameof(NovaSenha), ErrorMessage = "As senhas não conferem.")]
    public string ConfirmarSenha { get; set; } = string.Empty;
}

public class PerfilViewModel
{
    [Display(Name = "Nome completo")]
    [Required(ErrorMessage = "Informe o nome completo.")]
    [StringLength(160)]
    public string NomeCompleto { get; set; } = string.Empty;

    [Display(Name = "Cargo")]
    [StringLength(120)]
    public string? Cargo { get; set; }

    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    public string? Perfil { get; set; }

    public DateTime? UltimoAcesso { get; set; }

    public AlterarSenhaViewModel AlterarSenha { get; set; } = new();
}

// ------------------------------------------------------------------ Cadastros e auditoria

public class CadastrosViewModel
{
    public IReadOnlyList<Departamento> Departamentos { get; init; } = Array.Empty<Departamento>();
    public IReadOnlyList<Localizacao> Localizacoes { get; init; } = Array.Empty<Localizacao>();
    public IReadOnlyList<Fornecedor> Fornecedores { get; init; } = Array.Empty<Fornecedor>();
    public string AbaAtiva { get; init; } = "departamentos";
}

public class AuditoriaViewModel
{
    public ResultadoPaginado<RegistroAuditoria> Pagina { get; init; } = ResultadoPaginado<RegistroAuditoria>.Vazio();
    public string? Busca { get; init; }
    public TipoAcaoAuditoria? Acao { get; init; }
    public DateTime? De { get; init; }
    public DateTime? Ate { get; init; }
}

public class ErroViewModel
{
    public int CodigoStatus { get; init; } = 500;
    public string Titulo { get; init; } = "Algo deu errado";
    public string Mensagem { get; init; } = "Ocorreu um erro inesperado ao processar sua solicitação.";
    public string? RequestId { get; init; }
    public bool MostrarRequestId => !string.IsNullOrEmpty(RequestId);
}
