using Financeiro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Financeiro.Infrastructure.Persistence.Configurations.Common;

public sealed class FinancialSpaceConfiguration : IEntityTypeConfiguration<FinancialSpace>
{
    public void Configure(EntityTypeBuilder<FinancialSpace> b)
    {
        b.HasBaseType((Type?)null);
        b.ToTable("espacos");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasIndex(x => new { x.TenantId, x.NormalizedName }).IsUnique();
    }
}
