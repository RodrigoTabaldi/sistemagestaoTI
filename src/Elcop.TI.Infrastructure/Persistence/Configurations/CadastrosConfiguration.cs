using Elcop.TI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elcop.TI.Infrastructure.Persistence.Configurations;

public class DepartamentoConfiguration : IEntityTypeConfiguration<Departamento>
{
    public void Configure(EntityTypeBuilder<Departamento> builder)
    {
        builder.ToTable("Departamentos");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Nome).HasMaxLength(120).IsRequired();
        builder.HasIndex(d => d.Nome);
        builder.HasQueryFilter(d => !d.Excluido);
    }
}

public class LocalizacaoConfiguration : IEntityTypeConfiguration<Localizacao>
{
    public void Configure(EntityTypeBuilder<Localizacao> builder)
    {
        builder.ToTable("Localizacoes");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Nome).HasMaxLength(120).IsRequired();
        builder.Property(l => l.Uf).HasMaxLength(2);
        builder.HasIndex(l => new { l.Unidade, l.Nome });
        builder.HasQueryFilter(l => !l.Excluido);
    }
}

public class FornecedorConfiguration : IEntityTypeConfiguration<Fornecedor>
{
    public void Configure(EntityTypeBuilder<Fornecedor> builder)
    {
        builder.ToTable("Fornecedores");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Nome).HasMaxLength(160).IsRequired();
        builder.Property(f => f.Cnpj).HasMaxLength(18);
        builder.HasIndex(f => f.Nome);
        builder.HasQueryFilter(f => !f.Excluido);
    }
}

public class RegistroAuditoriaConfiguration : IEntityTypeConfiguration<RegistroAuditoria>
{
    public void Configure(EntityTypeBuilder<RegistroAuditoria> builder)
    {
        builder.ToTable("RegistrosAuditoria");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Usuario).HasMaxLength(160).IsRequired();
        builder.Property(r => r.Entidade).HasMaxLength(80).IsRequired();
        builder.Property(r => r.Descricao).HasMaxLength(1000).IsRequired();
        builder.HasIndex(r => r.DataHora);
        builder.HasIndex(r => new { r.Entidade, r.EntidadeId });
    }
}
