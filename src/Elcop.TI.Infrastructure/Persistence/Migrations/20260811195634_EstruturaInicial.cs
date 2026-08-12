using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elcop.TI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EstruturaInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Departamentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Sigla = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    CentroCusto = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Responsavel = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    Habilitado = table.Column<bool>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CriadoPor = table.Column<string>(type: "TEXT", nullable: true),
                    AtualizadoEm = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AtualizadoPor = table.Column<string>(type: "TEXT", nullable: true),
                    Excluido = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departamentos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Fornecedores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Cnpj = table.Column<string>(type: "TEXT", maxLength: 18, nullable: true),
                    Contato = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    Telefone = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    Observacoes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Habilitado = table.Column<bool>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CriadoPor = table.Column<string>(type: "TEXT", nullable: true),
                    AtualizadoEm = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AtualizadoPor = table.Column<string>(type: "TEXT", nullable: true),
                    Excluido = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fornecedores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Localizacoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Unidade = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    Endereco = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Cidade = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    Uf = table.Column<string>(type: "TEXT", maxLength: 2, nullable: true),
                    Habilitado = table.Column<bool>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CriadoPor = table.Column<string>(type: "TEXT", nullable: true),
                    AtualizadoEm = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AtualizadoPor = table.Column<string>(type: "TEXT", nullable: true),
                    Excluido = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Localizacoes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Perfis",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Perfis", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegistrosAuditoria",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DataHora = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Usuario = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Acao = table.Column<int>(type: "INTEGER", nullable: false),
                    Entidade = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    EntidadeId = table.Column<int>(type: "INTEGER", nullable: true),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    EnderecoIp = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosAuditoria", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    NomeCompleto = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Cargo = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    Habilitado = table.Column<bool>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UltimoAcesso = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Tema = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: true),
                    SecurityStamp = table.Column<string>(type: "TEXT", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumber = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Colaboradores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NomeCompleto = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Matricula = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Cpf = table.Column<string>(type: "TEXT", maxLength: 14, nullable: true),
                    Rg = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    EmailPessoal = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    Telefone = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    Celular = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    Cargo = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    GestorImediato = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    DepartamentoId = table.Column<int>(type: "INTEGER", nullable: true),
                    LocalizacaoId = table.Column<int>(type: "INTEGER", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    DataAdmissao = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DataDesligamento = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Observacoes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    FotoUrl = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CriadoPor = table.Column<string>(type: "TEXT", nullable: true),
                    AtualizadoEm = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AtualizadoPor = table.Column<string>(type: "TEXT", nullable: true),
                    Excluido = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Colaboradores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Colaboradores_Departamentos_DepartamentoId",
                        column: x => x.DepartamentoId,
                        principalTable: "Departamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Colaboradores_Localizacoes_LocalizacaoId",
                        column: x => x.LocalizacaoId,
                        principalTable: "Localizacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PerfilClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerfilClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PerfilClaims_Perfis_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Perfis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsuarioClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsuarioClaims_Usuarios_UserId",
                        column: x => x.UserId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsuarioLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderKey = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_UsuarioLogins_Usuarios_UserId",
                        column: x => x.UserId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsuarioPerfis",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioPerfis", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UsuarioPerfis_Perfis_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Perfis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsuarioPerfis_Usuarios_UserId",
                        column: x => x.UserId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsuarioTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_UsuarioTokens_Usuarios_UserId",
                        column: x => x.UserId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Ativos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Patrimonio = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    Marca = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Modelo = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    NumeroSerie = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    Imei = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Imei2 = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    NumeroLinha = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    Operadora = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    EnderecoMac = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Cor = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Etiqueta = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Condicao = table.Column<int>(type: "INTEGER", nullable: false),
                    DepartamentoId = table.Column<int>(type: "INTEGER", nullable: true),
                    LocalizacaoId = table.Column<int>(type: "INTEGER", nullable: true),
                    ColaboradorAtualId = table.Column<int>(type: "INTEGER", nullable: true),
                    FornecedorId = table.Column<int>(type: "INTEGER", nullable: true),
                    DataAquisicao = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ValorAquisicao = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NotaFiscal = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    GarantiaAte = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Contrato = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    Processador = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    MemoriaRam = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    Armazenamento = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    SistemaOperacional = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    Hostname = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    Tamanho = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    Acessorios = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Observacoes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    FotoUrl = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CriadoPor = table.Column<string>(type: "TEXT", nullable: true),
                    AtualizadoEm = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AtualizadoPor = table.Column<string>(type: "TEXT", nullable: true),
                    Excluido = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ativos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ativos_Colaboradores_ColaboradorAtualId",
                        column: x => x.ColaboradorAtualId,
                        principalTable: "Colaboradores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ativos_Departamentos_DepartamentoId",
                        column: x => x.DepartamentoId,
                        principalTable: "Departamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Ativos_Fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Ativos_Localizacoes_LocalizacaoId",
                        column: x => x.LocalizacaoId,
                        principalTable: "Localizacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Demandas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Titulo = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 5000, nullable: false),
                    Categoria = table.Column<int>(type: "INTEGER", nullable: false),
                    Prioridade = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    SolicitanteId = table.Column<int>(type: "INTEGER", nullable: true),
                    DepartamentoId = table.Column<int>(type: "INTEGER", nullable: true),
                    AtivoId = table.Column<int>(type: "INTEGER", nullable: true),
                    Responsavel = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    DataAbertura = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PrazoLimite = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DataInicio = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DataConclusao = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PercentualConclusao = table.Column<int>(type: "INTEGER", nullable: false),
                    TempoGastoMinutos = table.Column<int>(type: "INTEGER", nullable: false),
                    Ordem = table.Column<int>(type: "INTEGER", nullable: false),
                    Tags = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Solucao = table.Column<string>(type: "TEXT", maxLength: 5000, nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CriadoPor = table.Column<string>(type: "TEXT", nullable: true),
                    AtualizadoEm = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AtualizadoPor = table.Column<string>(type: "TEXT", nullable: true),
                    Excluido = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Demandas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Demandas_Ativos_AtivoId",
                        column: x => x.AtivoId,
                        principalTable: "Ativos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Demandas_Colaboradores_SolicitanteId",
                        column: x => x.SolicitanteId,
                        principalTable: "Colaboradores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Demandas_Departamentos_DepartamentoId",
                        column: x => x.DepartamentoId,
                        principalTable: "Departamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Movimentacoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Protocolo = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    AtivoId = table.Column<int>(type: "INTEGER", nullable: false),
                    ColaboradorId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    DataRetirada = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DataPrevistaDevolucao = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CondicaoRetirada = table.Column<int>(type: "INTEGER", nullable: false),
                    AcessoriosEntregues = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ResponsavelEntrega = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    LocalEntrega = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    ObservacoesRetirada = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    TermoAssinado = table.Column<bool>(type: "INTEGER", nullable: false),
                    DataDevolucao = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CondicaoDevolucao = table.Column<int>(type: "INTEGER", nullable: true),
                    AcessoriosDevolvidos = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ResponsavelRecebimento = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    ObservacoesDevolucao = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    ComAvaria = table.Column<bool>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CriadoPor = table.Column<string>(type: "TEXT", nullable: true),
                    AtualizadoEm = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AtualizadoPor = table.Column<string>(type: "TEXT", nullable: true),
                    Excluido = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Movimentacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Movimentacoes_Ativos_AtivoId",
                        column: x => x.AtivoId,
                        principalTable: "Ativos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Movimentacoes_Colaboradores_ColaboradorId",
                        column: x => x.ColaboradorId,
                        principalTable: "Colaboradores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DemandaAndamentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DemandaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Autor = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    Data = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StatusAnterior = table.Column<int>(type: "INTEGER", nullable: true),
                    StatusNovo = table.Column<int>(type: "INTEGER", nullable: true),
                    TempoGastoMinutos = table.Column<int>(type: "INTEGER", nullable: false),
                    PercentualInformado = table.Column<int>(type: "INTEGER", nullable: true),
                    Automatico = table.Column<bool>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CriadoPor = table.Column<string>(type: "TEXT", nullable: true),
                    AtualizadoEm = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AtualizadoPor = table.Column<string>(type: "TEXT", nullable: true),
                    Excluido = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandaAndamentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DemandaAndamentos_Demandas_DemandaId",
                        column: x => x.DemandaId,
                        principalTable: "Demandas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ativos_ColaboradorAtualId",
                table: "Ativos",
                column: "ColaboradorAtualId");

            migrationBuilder.CreateIndex(
                name: "IX_Ativos_DepartamentoId",
                table: "Ativos",
                column: "DepartamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Ativos_Excluido",
                table: "Ativos",
                column: "Excluido");

            migrationBuilder.CreateIndex(
                name: "IX_Ativos_FornecedorId",
                table: "Ativos",
                column: "FornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_Ativos_Imei",
                table: "Ativos",
                column: "Imei");

            migrationBuilder.CreateIndex(
                name: "IX_Ativos_LocalizacaoId",
                table: "Ativos",
                column: "LocalizacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Ativos_NumeroSerie",
                table: "Ativos",
                column: "NumeroSerie");

            migrationBuilder.CreateIndex(
                name: "IX_Ativos_Patrimonio",
                table: "Ativos",
                column: "Patrimonio",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ativos_Tipo_Status",
                table: "Ativos",
                columns: new[] { "Tipo", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Colaboradores_DepartamentoId",
                table: "Colaboradores",
                column: "DepartamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Colaboradores_Email",
                table: "Colaboradores",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Colaboradores_LocalizacaoId",
                table: "Colaboradores",
                column: "LocalizacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Colaboradores_Matricula",
                table: "Colaboradores",
                column: "Matricula",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Colaboradores_NomeCompleto",
                table: "Colaboradores",
                column: "NomeCompleto");

            migrationBuilder.CreateIndex(
                name: "IX_Colaboradores_Status",
                table: "Colaboradores",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DemandaAndamentos_DemandaId_Data",
                table: "DemandaAndamentos",
                columns: new[] { "DemandaId", "Data" });

            migrationBuilder.CreateIndex(
                name: "IX_Demandas_AtivoId",
                table: "Demandas",
                column: "AtivoId");

            migrationBuilder.CreateIndex(
                name: "IX_Demandas_Codigo",
                table: "Demandas",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Demandas_DataAbertura",
                table: "Demandas",
                column: "DataAbertura");

            migrationBuilder.CreateIndex(
                name: "IX_Demandas_DepartamentoId",
                table: "Demandas",
                column: "DepartamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Demandas_PrazoLimite",
                table: "Demandas",
                column: "PrazoLimite");

            migrationBuilder.CreateIndex(
                name: "IX_Demandas_SolicitanteId",
                table: "Demandas",
                column: "SolicitanteId");

            migrationBuilder.CreateIndex(
                name: "IX_Demandas_Status_Prioridade",
                table: "Demandas",
                columns: new[] { "Status", "Prioridade" });

            migrationBuilder.CreateIndex(
                name: "IX_Departamentos_Nome",
                table: "Departamentos",
                column: "Nome");

            migrationBuilder.CreateIndex(
                name: "IX_Fornecedores_Nome",
                table: "Fornecedores",
                column: "Nome");

            migrationBuilder.CreateIndex(
                name: "IX_Localizacoes_Unidade_Nome",
                table: "Localizacoes",
                columns: new[] { "Unidade", "Nome" });

            migrationBuilder.CreateIndex(
                name: "IX_Movimentacoes_AtivoId_Status",
                table: "Movimentacoes",
                columns: new[] { "AtivoId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Movimentacoes_ColaboradorId_Status",
                table: "Movimentacoes",
                columns: new[] { "ColaboradorId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Movimentacoes_DataPrevistaDevolucao",
                table: "Movimentacoes",
                column: "DataPrevistaDevolucao");

            migrationBuilder.CreateIndex(
                name: "IX_Movimentacoes_DataRetirada",
                table: "Movimentacoes",
                column: "DataRetirada");

            migrationBuilder.CreateIndex(
                name: "IX_Movimentacoes_Protocolo",
                table: "Movimentacoes",
                column: "Protocolo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PerfilClaims_RoleId",
                table: "PerfilClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "Perfis",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAuditoria_DataHora",
                table: "RegistrosAuditoria",
                column: "DataHora");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAuditoria_Entidade_EntidadeId",
                table: "RegistrosAuditoria",
                columns: new[] { "Entidade", "EntidadeId" });

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioClaims_UserId",
                table: "UsuarioClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioLogins_UserId",
                table: "UsuarioLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioPerfis_RoleId",
                table: "UsuarioPerfis",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "Usuarios",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "Usuarios",
                column: "NormalizedUserName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DemandaAndamentos");

            migrationBuilder.DropTable(
                name: "Movimentacoes");

            migrationBuilder.DropTable(
                name: "PerfilClaims");

            migrationBuilder.DropTable(
                name: "RegistrosAuditoria");

            migrationBuilder.DropTable(
                name: "UsuarioClaims");

            migrationBuilder.DropTable(
                name: "UsuarioLogins");

            migrationBuilder.DropTable(
                name: "UsuarioPerfis");

            migrationBuilder.DropTable(
                name: "UsuarioTokens");

            migrationBuilder.DropTable(
                name: "Demandas");

            migrationBuilder.DropTable(
                name: "Perfis");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Ativos");

            migrationBuilder.DropTable(
                name: "Colaboradores");

            migrationBuilder.DropTable(
                name: "Fornecedores");

            migrationBuilder.DropTable(
                name: "Departamentos");

            migrationBuilder.DropTable(
                name: "Localizacoes");
        }
    }
}
