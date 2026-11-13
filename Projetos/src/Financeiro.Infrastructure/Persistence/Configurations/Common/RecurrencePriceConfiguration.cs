using Financeiro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Financeiro.Infrastructure.Persistence.Configurations.Common;

public sealed class RecurrencePriceConfiguration : IEntityTypeConfiguration<RecurrencePrice>
{
    public void Configure(EntityTypeBuilder<RecurrencePrice> b)
    {
        b.HasBaseType((Type?)null);
        b.ToTable("recorrencia_vigencias");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasAlternateKey(x => new { x.TenantId, x.SpaceId, x.Id });
        b.HasOne<FinancialSpace>().WithMany().HasForeignKey(x => new { x.TenantId, x.SpaceId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Recurrence>().WithMany().HasForeignKey(x => new { x.TenantId, x.SpaceId, x.RecurrenceId }).HasPrincipalKey(x => new { x.TenantId, x.SpaceId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.TenantId, x.SpaceId, x.RecurrenceId, x.StartDate }).IsUnique();
    }
}
