using Financeiro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Financeiro.Infrastructure.Persistence.Configurations.Common;

public sealed class InformedReferenceConfiguration : IEntityTypeConfiguration<InformedReference>
{
    public void Configure(EntityTypeBuilder<InformedReference> b)
    {
        b.HasBaseType((Type?)null);
        b.ToTable("referencias_informadas");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasAlternateKey(x => new { x.TenantId, x.SpaceId, x.Id });
        b.HasOne<FinancialSpace>().WithMany().HasForeignKey(x => new { x.TenantId, x.SpaceId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<PayableTitle>().WithMany().HasForeignKey(x => new { x.TenantId, x.SpaceId, x.TitleId }).HasPrincipalKey(x => new { x.TenantId, x.SpaceId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Obligation>().WithMany().HasForeignKey(x => new { x.TenantId, x.SpaceId, x.ObligationId }).HasPrincipalKey(x => new { x.TenantId, x.SpaceId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<TravelBudget>().WithMany().HasForeignKey(x => new { x.TenantId, x.SpaceId, x.TravelId }).HasPrincipalKey(x => new { x.TenantId, x.SpaceId, x.Id }).OnDelete(DeleteBehavior.Restrict);
    }
}
