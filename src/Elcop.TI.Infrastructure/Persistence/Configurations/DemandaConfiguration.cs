using Elcop.TI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elcop.TI.Infrastructure.Persistence.Configurations;

public class DemandaConfiguration : IEntityTypeConfiguration<Demanda>
{
    public void Configure(EntityTypeBuilder<Demanda> builder)
    {
        builder.ToTable("Demandas");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Codigo).HasMaxLength(30).IsRequired();
        builder.Property(d => d.Titulo).HasMaxLength(180).IsRequired();
        builder.Property(d => d.Descricao).IsRequired();

        builder.HasIndex(d => d.Codigo).IsUnique();
        builder.HasIndex(d => new { d.Status, d.Prioridade });
        builder.HasIndex(d => d.DataAbertura);
        builder.HasIndex(d => d.PrazoLimite);

        builder.HasOne(d => d.Solicitante)
               .WithMany(c => c.DemandasSolicitadas)
               .HasForeignKey(d => d.SolicitanteId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(d => d.Departamento)
               .WithMany()
               .HasForeignKey(d => d.DepartamentoId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(d => d.Ativo)
               .WithMany(a => a.Demandas)
               .HasForeignKey(d => d.AtivoId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(d => d.Andamentos)
               .WithOne(a => a.Demanda)
               .HasForeignKey(a => a.DemandaId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(d => !d.Excluido);
    }
}

public class DemandaAndamentoConfiguration : IEntityTypeConfiguration<DemandaAndamento>
{
    public void Configure(EntityTypeBuilder<DemandaAndamento> builder)
    {
        builder.ToTable("DemandaAndamentos");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Descricao).IsRequired();
        builder.HasIndex(a => new { a.DemandaId, a.Data });

        builder.HasQueryFilter(a => !a.Excluido);
    }
}
