using Financeiro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Financeiro.Infrastructure.Persistence.Configurations.Common;

public sealed class ImportLinkConfiguration : IEntityTypeConfiguration<ImportLink>
{
    public void Configure(EntityTypeBuilder<ImportLink> b)
    {
        b.HasBaseType((Type?)null);
        b.ToTable("vinculos_origem");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Version).IsConcurrencyToken();
        b.Property(x => x.EntityType).HasMaxLength(50).IsRequired();
        b.Property(x => x.Role).HasMaxLength(30).IsRequired();
        b.Property(x => x.OccurrenceKey).HasMaxLength(100).IsRequired();
        b.HasAlternateKey(x => new { x.TenantId, x.SpaceId, x.Id });
        b.HasOne<FinancialSpace>().WithMany().HasForeignKey(x => new { x.TenantId, x.SpaceId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ImportBatch>().WithMany().HasForeignKey(x => new { x.TenantId, x.SpaceId, x.ImportBatchId }).HasPrincipalKey(x => new { x.TenantId, x.SpaceId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ImportSourceRecord>().WithMany().HasForeignKey(x => new { x.TenantId, x.SpaceId, x.SourceRecordId }).HasPrincipalKey(x => new { x.TenantId, x.SpaceId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.ImportBatchId, x.SourceRecordId, x.EntityType, x.EntityId, x.Role, x.OccurrenceKey }).IsUnique();
    }
}
