using Financeiro.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Migrations.Tests;

[Trait("Category", "Database")]
public class CardBrandMigrationTests
{
    [Fact]
    public async Task GlobalBrandsAreCopiedPerReferencedScopeWithoutDeletingLegacyEvidence()
    {
        await using var database = await TestDatabase.CreateAsync("AccessAuditIndexes");
        await using var db = database.Open();
        var tenant = new Tenant { Name = "One", PublicIdentifier = Guid.NewGuid().ToString("N") };
        var otherTenant = new Tenant { Name = "Two", PublicIdentifier = Guid.NewGuid().ToString("N") };
        var spaces = new[]
        {
            new FinancialSpace { TenantId = tenant.Id, Name = "First", NormalizedName = "FIRST" },
            new FinancialSpace { TenantId = tenant.Id, Name = "Second", NormalizedName = "SECOND" },
            new FinancialSpace { TenantId = otherTenant.Id, Name = "Third", NormalizedName = "THIRD" }
        };
        db.AddRange(tenant, otherTenant);
        db.AddRange(spaces);
        await db.SaveChangesAsync();
        var brand = Guid.NewGuid();
        var unused = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO bandeiras (id,name,version,created_at,updated_at) VALUES ({brand},{"Visa"},7,{now},{now}),({unused},{"Unassigned"},1,{now},{now})");
        foreach (var space in spaces)
        {
            var institution = new Institution { TenantId = space.TenantId, SpaceId = space.Id, Name = "Bank" };
            db.Add(institution);
            db.Add(new CreditCard { TenantId = space.TenantId, SpaceId = space.Id, InstitutionId = institution.Id, BrandId = brand, Name = "Card " + space.Name });
        }
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        await db.Database.MigrateAsync();
        var brands = await db.CardBrands.ToArrayAsync();
        Assert.Equal(3, brands.Length);
        Assert.All(brands, x => { Assert.Equal("Visa", x.Name); Assert.True(x.Active); Assert.Equal(7, x.Version); });
        Assert.Equal(3, brands.Select(x => x.Id).Distinct().Count());
        foreach (var card in await db.CreditCards.ToArrayAsync())
            Assert.Equal(brands.Single(x => x.TenantId == card.TenantId && x.SpaceId == card.SpaceId).Id, card.BrandId);
        Assert.Equal(2, await db.Database.SqlQuery<int>($"SELECT CAST(COUNT(*) AS int) AS \"Value\" FROM bandeiras").SingleAsync());
        Assert.DoesNotContain(brands, x => x.Name == "Unassigned");
        Assert.False(db.Database.HasPendingModelChanges());
        await db.Database.MigrateAsync();
        Assert.Equal(3, await db.CardBrands.CountAsync());
    }
}
