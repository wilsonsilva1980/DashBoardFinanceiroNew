using Financeiro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Financeiro.Infrastructure.Persistence.Configurations.Common;

public sealed class CardInvoiceConfiguration : IEntityTypeConfiguration<CardInvoice>
{
    public void Configure(EntityTypeBuilder<CardInvoice> b)
    {
        b.HasBaseType((Type?)null);
        b.ToTable("faturas");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasAlternateKey(x => new { x.TenantId, x.SpaceId, x.Id });
        b.HasOne<FinancialSpace>().WithMany().HasForeignKey(x => new { x.TenantId, x.SpaceId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<CreditCard>().WithMany().HasForeignKey(x => new { x.TenantId, x.SpaceId, x.CardId }).HasPrincipalKey(x => new { x.TenantId, x.SpaceId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<PayableTitle>().WithMany().HasForeignKey(x => new { x.TenantId, x.SpaceId, x.TitleId }).HasPrincipalKey(x => new { x.TenantId, x.SpaceId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.TenantId, x.SpaceId, x.CardId, x.Period }).IsUnique();
        b.HasIndex(x => new { x.TenantId, x.SpaceId, x.TitleId }).IsUnique();
    }
}
