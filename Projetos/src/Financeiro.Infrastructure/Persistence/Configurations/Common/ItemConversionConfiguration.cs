using Financeiro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Financeiro.Infrastructure.Persistence.Configurations.Common;

public sealed class ItemConversionConfiguration : IEntityTypeConfiguration<ItemConversion>
{
    public void Configure(EntityTypeBuilder<ItemConversion> b)
    {
        b.HasBaseType((Type?)null);
        b.ToTable("item_conversoes");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasAlternateKey(x => new { x.TenantId, x.SpaceId, x.Id });
        b.HasOne<FinancialSpace>().WithMany().HasForeignKey(x => new { x.TenantId, x.SpaceId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<TravelItem>().WithMany().HasForeignKey(x => new { x.TenantId, x.SpaceId, x.ItemId }).HasPrincipalKey(x => new { x.TenantId, x.SpaceId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ExchangeRate>().WithMany().HasForeignKey(x => new { x.TenantId, x.SpaceId, x.RateId }).HasPrincipalKey(x => new { x.TenantId, x.SpaceId, x.Id }).OnDelete(DeleteBehavior.Restrict);
    }
}
