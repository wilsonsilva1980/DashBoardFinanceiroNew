using Financeiro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Financeiro.Infrastructure.Persistence.Configurations.Common;

public sealed class InvoicePurchaseConfiguration : IEntityTypeConfiguration<InvoicePurchase>
{
    public void Configure(EntityTypeBuilder<InvoicePurchase> b)
    {
        b.HasBaseType((Type?)null);
        b.ToTable("fatura_itens");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasAlternateKey(x => new { x.TenantId, x.SpaceId, x.Id });
        b.HasOne<FinancialSpace>().WithMany().HasForeignKey(x => new { x.TenantId, x.SpaceId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<CardInvoice>().WithMany().HasForeignKey(x => new { x.TenantId, x.SpaceId, x.InvoiceId }).HasPrincipalKey(x => new { x.TenantId, x.SpaceId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Obligation>().WithMany().HasForeignKey(x => new { x.TenantId, x.SpaceId, x.PurchaseObligationId }).HasPrincipalKey(x => new { x.TenantId, x.SpaceId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.TenantId, x.SpaceId, x.InvoiceId, x.OccurrenceKey }).IsUnique();
    }
}
