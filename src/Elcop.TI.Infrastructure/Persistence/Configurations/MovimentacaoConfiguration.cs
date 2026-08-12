using Elcop.TI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elcop.TI.Infrastructure.Persistence.Configurations;

public class MovimentacaoConfiguration : IEntityTypeConfiguration<Movimentacao>
{
    public void Configure(EntityTypeBuilder<Movimentacao> builder)
    {
        builder.ToTable("Movimentacoes");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Protocolo).HasMaxLength(30).IsRequired();

        builder.HasIndex(m => m.Protocolo).IsUnique();
        builder.HasIndex(m => new { m.AtivoId, m.Status });
        builder.HasIndex(m => new { m.ColaboradorId, m.Status });
        builder.HasIndex(m => m.DataRetirada);
        builder.HasIndex(m => m.DataPrevistaDevolucao);

        // Histórico de posse não pode ser apagado em cascata junto do ativo/colaborador.
        builder.HasOne(m => m.Ativo)
               .WithMany(a => a.Movimentacoes)
               .HasForeignKey(m => m.AtivoId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Colaborador)
               .WithMany(c => c.Movimentacoes)
               .HasForeignKey(m => m.ColaboradorId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(m => !m.Excluido);
    }
}
