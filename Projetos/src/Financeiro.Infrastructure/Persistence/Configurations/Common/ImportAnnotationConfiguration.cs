using Financeiro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Financeiro.Infrastructure.Persistence.Configurations.Common;

public sealed class ImportAnnotationConfiguration : IEntityTypeConfiguration<ImportAnnotation>
{
    public void Configure(EntityTypeBuilder<ImportAnnotation> b)
    {
        b.HasBaseType((Type?)null);
        b.ToTable("anotacoes_origem");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Version).IsConcurrencyToken();
        b.Property(x => x.SheetName).HasMaxLength(100).IsRequired();
        b.Property(x => x.CellReference).HasMaxLength(20).IsRequired();
        b.Property(x => x.SourceAuthor).HasMaxLength(200);
        b.Property(x => x.Text).HasMaxLength(8000).IsRequired();
        b.HasAlternateKey(x => new { x.TenantId, x.SpaceId, x.Id });
        b.HasOne<FinancialSpace>().WithMany().HasForeignKey(x => new { x.TenantId, x.SpaceId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ImportBatch>().WithMany().HasForeignKey(x => new { x.TenantId, x.SpaceId, x.ImportBatchId }).HasPrincipalKey(x => new { x.TenantId, x.SpaceId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.ImportBatchId, x.SheetName, x.CellReference });
    }
}
