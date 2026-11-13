using Financeiro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Financeiro.Infrastructure.Persistence.Configurations.Common;

public sealed class ObligationConfiguration : IEntityTypeConfiguration<Obligation>
{
    public void Configure(EntityTypeBuilder<Obligation> b)
    {
        b.HasBaseType((Type?)null);
        b.ToTable("obrigacoes");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasAlternateKey(x => new { x.TenantId, x.SpaceId, x.Id });
        b.HasOne<FinancialSpace>().WithMany().HasForeignKey(x => new { x.TenantId, x.SpaceId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Category>().WithMany().HasForeignKey(x => new { x.TenantId, x.SpaceId, x.CategoryId }).HasPrincipalKey(x => new { x.TenantId, x.SpaceId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Person>().WithMany().HasForeignKey(x => new { x.TenantId, x.SpaceId, x.ResponsibleId }).HasPrincipalKey(x => new { x.TenantId, x.SpaceId, x.Id }).OnDelete(DeleteBehavior.Restrict);
    }
}
