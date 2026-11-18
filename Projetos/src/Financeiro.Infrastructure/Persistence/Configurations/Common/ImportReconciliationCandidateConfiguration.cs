using Financeiro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Financeiro.Infrastructure.Persistence.Configurations.Common;

public sealed class ImportReconciliationCandidateConfiguration : IEntityTypeConfiguration<ImportReconciliationCandidate>
{
    public void Configure(EntityTypeBuilder<ImportReconciliationCandidate> b)
    {
        b.HasBaseType((Type?)null);
        b.ToTable("import_reconciliation_candidates");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Version).IsConcurrencyToken();
        b.Property(x => x.MatchKind).HasMaxLength(50).IsRequired();
        b.Property(x => x.Confidence).HasPrecision(5, 4);
        b.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        b.HasAlternateKey(x => new { x.TenantId, x.SpaceId, x.Id });
        b.HasOne<FinancialSpace>().WithMany().HasForeignKey(x => new { x.TenantId, x.SpaceId })
            .HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ImportBatch>().WithMany().HasForeignKey(x => new { x.TenantId, x.SpaceId, x.ImportBatchId })
            .HasPrincipalKey(x => new { x.TenantId, x.SpaceId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ImportSourceRecord>().WithMany().HasForeignKey(x => new { x.TenantId, x.SpaceId, x.SourceRecordId })
            .HasPrincipalKey(x => new { x.TenantId, x.SpaceId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ImportSourceRecord>().WithMany().HasForeignKey(x => new { x.TenantId, x.SpaceId, x.CandidateRecordId })
            .HasPrincipalKey(x => new { x.TenantId, x.SpaceId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.ImportBatchId, x.SourceRecordId, x.CandidateRecordId }).IsUnique();
    }
}
