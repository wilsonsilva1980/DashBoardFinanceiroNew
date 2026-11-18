using Financeiro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Financeiro.Infrastructure.Persistence.Configurations.Common;

public sealed class ImportCorrectionConfiguration : IEntityTypeConfiguration<ImportCorrection>
{
    public void Configure(EntityTypeBuilder<ImportCorrection> b)
    {
        b.HasBaseType((Type?)null);
        b.ToTable("import_corrections");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Version).IsConcurrencyToken();
        b.Property(x => x.FieldName).HasMaxLength(100).IsRequired();
        b.Property(x => x.OriginalValueJson).HasMaxLength(8000);
        b.Property(x => x.CorrectedValueJson).HasMaxLength(8000);
        b.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        b.HasAlternateKey(x => new { x.TenantId, x.SpaceId, x.Id });
        b.HasOne<FinancialSpace>().WithMany().HasForeignKey(x => new { x.TenantId, x.SpaceId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ImportBatch>().WithMany().HasForeignKey(x => new { x.TenantId, x.SpaceId, x.ImportBatchId })
            .HasPrincipalKey(x => new { x.TenantId, x.SpaceId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ImportSourceRecord>().WithMany().HasForeignKey(x => new { x.TenantId, x.SpaceId, x.ImportSourceRecordId })
            .HasPrincipalKey(x => new { x.TenantId, x.SpaceId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.ImportSourceRecordId, x.FieldName, x.CreatedAt });
    }
}
