using Financeiro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Financeiro.Infrastructure.Persistence.Configurations.Common;

public sealed class TenantMemberConfiguration : IEntityTypeConfiguration<TenantMember>
{
    public void Configure(EntityTypeBuilder<TenantMember> b)
    {
        b.HasBaseType((Type?)null);
        b.ToTable("membros_tenant");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasIndex(x => new { x.TenantId, x.UserId }).IsUnique();
    }
}
