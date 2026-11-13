using Financeiro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Financeiro.Infrastructure.Persistence.Configurations.Common;

public sealed class SpaceMemberConfiguration : IEntityTypeConfiguration<SpaceMember>
{
    public void Configure(EntityTypeBuilder<SpaceMember> b)
    {
        b.HasBaseType((Type?)null);
        b.ToTable("membros_espaco");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasAlternateKey(x => new { x.TenantId, x.SpaceId, x.Id });
        b.HasOne<FinancialSpace>().WithMany().HasForeignKey(x => new { x.TenantId, x.SpaceId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.TenantId, x.SpaceId, x.UserId }).IsUnique();
    }
}
