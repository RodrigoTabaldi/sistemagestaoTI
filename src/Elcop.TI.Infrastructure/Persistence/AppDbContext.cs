using System.Reflection;
using Elcop.TI.Application.Common;
using Elcop.TI.Domain.Common;
using Elcop.TI.Domain.Entities;
using Elcop.TI.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Elcop.TI.Infrastructure.Persistence;

/// <summary>
/// Contexto único da aplicação: dados de negócio + tabelas do ASP.NET Identity.
/// </summary>
public class AppDbContext : IdentityDbContext<ApplicationUser>, IAppDbContext
{
    private readonly IUsuarioAtual? _usuarioAtual;

    public AppDbContext(DbContextOptions<AppDbContext> options, IUsuarioAtual? usuarioAtual = null)
        : base(options)
    {
        _usuarioAtual = usuarioAtual;
    }

    /// <summary>
    /// Construtor para os contextos derivados por provedor (ver <see cref="AppDbContextPostgres"/>).
    /// Cada provedor precisa do próprio tipo de contexto porque o EF Core indexa as
    /// migrations pelo tipo — é isso que permite os conjuntos SQLite e Postgres conviverem
    /// no mesmo assembly.
    /// </summary>
    protected AppDbContext(DbContextOptions options, IUsuarioAtual? usuarioAtual = null)
        : base(options)
    {
        _usuarioAtual = usuarioAtual;
    }

    public DbSet<Ativo> Ativos => Set<Ativo>();
    public DbSet<Colaborador> Colaboradores => Set<Colaborador>();
    public DbSet<Departamento> Departamentos => Set<Departamento>();
    public DbSet<Localizacao> Localizacoes => Set<Localizacao>();
    public DbSet<Fornecedor> Fornecedores => Set<Fornecedor>();
    public DbSet<Movimentacao> Movimentacoes => Set<Movimentacao>();
    public DbSet<Demanda> Demandas => Set<Demanda>();
    public DbSet<DemandaAndamento> DemandaAndamentos => Set<DemandaAndamento>();
    public DbSet<RegistroAuditoria> RegistrosAuditoria => Set<RegistroAuditoria>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Nomes de tabela do Identity em português, para o banco ficar coerente.
        builder.Entity<ApplicationUser>().ToTable("Usuarios");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRole>().ToTable("Perfis");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>().ToTable("UsuarioPerfis");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<string>>().ToTable("UsuarioClaims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>().ToTable("UsuarioLogins");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>().ToTable("UsuarioTokens");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>>().ToTable("PerfilClaims");
    }

    public override int SaveChanges()
    {
        AplicarCarimboDeAuditoria();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        AplicarCarimboDeAuditoria();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Rede de segurança: garante CriadoEm/AtualizadoEm mesmo quando o serviço
    /// esquece de chamar MarcarCriacao/MarcarAtualizacao.
    /// </summary>
    private void AplicarCarimboDeAuditoria()
    {
        var usuario = _usuarioAtual?.NomeExibicao ?? _usuarioAtual?.NomeUsuario;

        foreach (var entrada in ChangeTracker.Entries<EntidadeBase>())
        {
            switch (entrada.State)
            {
                case EntityState.Added:
                    if (entrada.Entity.CriadoEm == default) entrada.Entity.CriadoEm = DateTime.Now;
                    entrada.Entity.CriadoPor ??= usuario;
                    break;

                case EntityState.Modified:
                    entrada.Entity.AtualizadoEm = DateTime.Now;
                    entrada.Entity.AtualizadoPor = usuario ?? entrada.Entity.AtualizadoPor;
                    entrada.Property(e => e.CriadoEm).IsModified = false;
                    entrada.Property(e => e.CriadoPor).IsModified = false;
                    break;
            }
        }
    }
}
