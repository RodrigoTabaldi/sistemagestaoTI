using Elcop.TI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elcop.TI.Infrastructure.Persistence.Configurations;

public class AtivoConfiguration : IEntityTypeConfiguration<Ativo>
{
    public void Configure(EntityTypeBuilder<Ativo> builder)
    {
        builder.ToTable("Ativos");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Patrimonio).HasMaxLength(40).IsRequired();
        builder.Property(a => a.Marca).HasMaxLength(80).IsRequired();
        builder.Property(a => a.Modelo).HasMaxLength(120).IsRequired();
        builder.Property(a => a.NumeroSerie).HasMaxLength(80);
        builder.Property(a => a.Imei).HasMaxLength(20);
        builder.Property(a => a.Imei2).HasMaxLength(20);
        builder.Property(a => a.ValorAquisicao).HasColumnType("decimal(18,2)");

        // O patrimônio é o identificador operacional do equipamento: precisa ser único
        // mesmo entre registros excluídos logicamente, para não reciclar números.
        builder.HasIndex(a => a.Patrimonio).IsUnique();
        builder.HasIndex(a => a.NumeroSerie);
        builder.HasIndex(a => a.Imei);
        builder.HasIndex(a => new { a.Tipo, a.Status });
        builder.HasIndex(a => a.ColaboradorAtualId);
        builder.HasIndex(a => a.Excluido);

        builder.HasOne(a => a.Departamento)
               .WithMany(d => d.Ativos)
               .HasForeignKey(a => a.DepartamentoId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.Localizacao)
               .WithMany(l => l.Ativos)
               .HasForeignKey(a => a.LocalizacaoId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.Fornecedor)
               .WithMany(f => f.Ativos)
               .HasForeignKey(a => a.FornecedorId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.ColaboradorAtual)
               .WithMany()
               .HasForeignKey(a => a.ColaboradorAtualId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(a => !a.Excluido);
    }
}
