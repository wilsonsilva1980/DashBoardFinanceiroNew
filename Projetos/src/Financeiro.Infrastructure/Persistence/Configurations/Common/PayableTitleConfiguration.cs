using Financeiro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Financeiro.Infrastructure.Persistence.Configurations.Common;

public sealed class PayableTitleConfiguration : IEntityTypeConfiguration<PayableTitle>
{
    public void Configure(EntityTypeBuilder<PayableTitle> b)
    {
        b.HasBaseType((Type?)null);
        b.ToTable("titulos");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasAlternateKey(x => new { x.TenantId, x.SpaceId, x.Id });
        b.HasOne<FinancialSpace>().WithMany().HasForeignKey(x => new { x.TenantId, x.SpaceId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Obligation>().WithMany().HasForeignKey(x => new { x.TenantId, x.SpaceId, x.ObligationId }).HasPrincipalKey(x => new { x.TenantId, x.SpaceId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Person>().WithMany().HasForeignKey(x => new { x.TenantId, x.SpaceId, x.ResponsibleId }).HasPrincipalKey(x => new { x.TenantId, x.SpaceId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<CreditCard>().WithMany().HasForeignKey(x => new { x.TenantId, x.SpaceId, x.CardId }).HasPrincipalKey(x => new { x.TenantId, x.SpaceId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.TenantId, x.SpaceId, x.ObligationId, x.OccurrenceKey }).IsUnique();
        b.HasIndex(x => new { x.TenantId, x.SpaceId, x.ObligationId, x.InstallmentNumber }).IsUnique().HasFilter("\"installment_number\" IS NOT NULL");
    }
}
