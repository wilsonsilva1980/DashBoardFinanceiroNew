using Financeiro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Financeiro.Infrastructure.Persistence.Configurations.Common;

public sealed class PaymentAllocationConfiguration : IEntityTypeConfiguration<PaymentAllocation>
{
    public void Configure(EntityTypeBuilder<PaymentAllocation> b)
    {
        b.HasBaseType((Type?)null);
        b.ToTable("alocacoes_pagamento");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasAlternateKey(x => new { x.TenantId, x.SpaceId, x.Id });
        b.HasOne<FinancialSpace>().WithMany().HasForeignKey(x => new { x.TenantId, x.SpaceId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Payment>().WithMany().HasForeignKey(x => new { x.TenantId, x.SpaceId, x.PaymentId }).HasPrincipalKey(x => new { x.TenantId, x.SpaceId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<PayableTitle>().WithMany().HasForeignKey(x => new { x.TenantId, x.SpaceId, x.TitleId }).HasPrincipalKey(x => new { x.TenantId, x.SpaceId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.TenantId, x.SpaceId, x.PaymentId, x.TitleId }).IsUnique();
    }
}
