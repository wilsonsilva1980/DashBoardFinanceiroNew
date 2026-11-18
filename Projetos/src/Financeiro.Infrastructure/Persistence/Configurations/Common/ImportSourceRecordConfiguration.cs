using Financeiro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Financeiro.Infrastructure.Persistence.Configurations.Common;

public sealed class ImportSourceRecordConfiguration : IEntityTypeConfiguration<ImportSourceRecord>
{
    public void Configure(EntityTypeBuilder<ImportSourceRecord> b)
    {
        b.HasBaseType((Type?)null);
        b.ToTable("registros_origem");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Version).IsConcurrencyToken();
        b.Property(x => x.SheetName).HasMaxLength(100).IsRequired();
        b.Property(x => x.NormalizedSheetName).HasMaxLength(100).IsRequired();
        b.Property(x => x.CellReference).HasMaxLength(20).IsRequired();
        b.Property(x => x.RawContentJson).IsRequired();
        b.Property(x => x.DerivedValuesJson).IsRequired();
        b.Property(x => x.IssueCode).HasMaxLength(50);
        b.Property(x => x.IssueMessage).HasMaxLength(1000);
        b.Property(x => x.PrevailingSource).HasMaxLength(100);
        b.HasAlternateKey(x => new { x.TenantId, x.SpaceId, x.Id });
        b.HasOne<FinancialSpace>().WithMany().HasForeignKey(x => new { x.TenantId, x.SpaceId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ImportBatch>().WithMany().HasForeignKey(x => new { x.TenantId, x.SpaceId, x.ImportBatchId }).HasPrincipalKey(x => new { x.TenantId, x.SpaceId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.ImportBatchId, x.Classification });
        b.HasIndex(x => new { x.ImportBatchId, x.Decision });
        b.HasIndex(x => new { x.ImportBatchId, x.Severity });
    }
}
