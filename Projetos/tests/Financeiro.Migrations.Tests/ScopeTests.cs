using Financeiro.Application.Access;
using Financeiro.Domain.Entities;
using Financeiro.Infrastructure.Access;
using Financeiro.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Migrations.Tests;

[Trait("Category", "Database")]
public class ScopeTests
{
    [Fact]
    public async Task RequestsFilterForeignTenantsAndSpacesAndValidateEveryWrite()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var setup = database.Open();
        var owner = new ApplicationUser { Id = Guid.NewGuid(), UserName = "one", SecurityStamp = "one-stamp" };
        var outsider = new ApplicationUser { Id = Guid.NewGuid(), UserName = "two", SecurityStamp = "two-stamp" };
        setup.Users.AddRange(owner, outsider);
        await setup.SaveChangesAsync();
        var first = await new OnboardingService(setup, new OnboardingTests.Actor(owner.Id)).OnboardAsync(new("One", "Main", "BRL", "one"));
        var foreign = await new OnboardingService(setup, new OnboardingTests.Actor(outsider.Id)).OnboardAsync(new("Two", "Main", "BRL", "two"));
        var secondSpace = new FinancialSpace { TenantId = first.TenantId, Name = "Second", NormalizedName = "SECOND" };
        var firstPerson = new Person { TenantId = first.TenantId, SpaceId = first.SpaceId, Name = "First" };
        var secondPerson = new Person { TenantId = first.TenantId, SpaceId = secondSpace.Id, Name = "Second" };
        var foreignPerson = new Person { TenantId = foreign.TenantId, SpaceId = foreign.SpaceId, Name = "Foreign" };
        var foreignCategory = new Category { TenantId = foreign.TenantId, SpaceId = foreign.SpaceId, Name = "Foreign category" };
        var now = DateTimeOffset.UtcNow;
        var session = new UserSession { UserId = owner.Id, CreatedAt = now, LastAccessAt = now, ExpiresAt = now.AddHours(1), AbsoluteExpiresAt = now.AddHours(8), SecurityVersion = owner.SecurityStamp!, ActiveTenantId = first.TenantId, ActiveSpaceId = first.SpaceId };
        setup.AddRange(secondSpace, firstPerson, secondPerson, foreignPerson, foreignCategory, session,
            new SpaceMember { TenantId = first.TenantId, SpaceId = secondSpace.Id, UserId = owner.Id });
        await setup.SaveChangesAsync();
        var scope = new FinancialScope();
        await using var db = database.Open(scope);
        Assert.Empty(await db.Persons.ToListAsync());
        var resolver = new ScopeResolver(db, scope);
        Assert.True(await resolver.ResolveAsync(session.Id, owner.Id));
        Assert.Equal(firstPerson.Id, (await db.Persons.SingleAsync()).Id);
        Assert.Null(await db.Persons.SingleOrDefaultAsync(x => x.Id == foreignPerson.Id));
        Assert.Null(await db.Persons.SingleOrDefaultAsync(x => x.Id == secondPerson.Id));
        foreach (var (tenantId, spaceId) in new[] { (foreign.TenantId, foreign.SpaceId), (first.TenantId, secondSpace.Id) })
        {
            db.Add(new Person { TenantId = tenantId, SpaceId = spaceId, Name = "Unauthorized" });
            await Assert.ThrowsAsync<AccessDeniedException>(() => db.SaveChangesAsync());
            db.ChangeTracker.Clear();
        }
        var bypassed = await db.Persons.IgnoreQueryFilters().SingleAsync(x => x.Id == foreignPerson.Id);
        bypassed.Name = "Cannot mutate through bypass";
        await Assert.ThrowsAsync<AccessDeniedException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        db.Add(new Obligation { TenantId = first.TenantId, SpaceId = first.SpaceId, Description = "Foreign FK", CategoryId = foreignCategory.Id });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        await Assert.ThrowsAsync<AccessDeniedException>(() => resolver.SelectAsync(session.Id, owner.Id, foreign.TenantId, foreign.SpaceId));
        await resolver.SelectAsync(session.Id, owner.Id, first.TenantId, secondSpace.Id);
        Assert.Equal(secondPerson.Id, (await db.Persons.SingleAsync()).Id);
        session.RevokedAt = DateTimeOffset.UtcNow;
        await setup.SaveChangesAsync();
        Assert.False(await resolver.ResolveAsync(session.Id, owner.Id));
        Assert.Empty(await db.Persons.ToListAsync());
    }
}
