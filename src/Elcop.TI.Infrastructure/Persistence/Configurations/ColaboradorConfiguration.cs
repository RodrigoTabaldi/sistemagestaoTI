using Elcop.TI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elcop.TI.Infrastructure.Persistence.Configurations;

public class ColaboradorConfiguration : IEntityTypeConfiguration<Colaborador>
{
    public void Configure(EntityTypeBuilder<Colaborador> builder)
    {
        builder.ToTable("Colaboradores");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.NomeCompleto).HasMaxLength(160).IsRequired();
        builder.Property(c => c.Matricula).HasMaxLength(30).IsRequired();
        builder.Property(c => c.Email).HasMaxLength(160).IsRequired();

        builder.HasIndex(c => c.Matricula).IsUnique();
        builder.HasIndex(c => c.Email).IsUnique();
        builder.HasIndex(c => c.NomeCompleto);
        builder.HasIndex(c => c.Status);

        builder.HasOne(c => c.Departamento)
               .WithMany(d => d.Colaboradores)
               .HasForeignKey(c => c.DepartamentoId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(c => c.Localizacao)
               .WithMany()
               .HasForeignKey(c => c.LocalizacaoId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(c => !c.Excluido);
    }
}
