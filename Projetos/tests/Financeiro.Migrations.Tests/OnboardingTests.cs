using Financeiro.Application.Access;
using Financeiro.Infrastructure.Access;
using Financeiro.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Migrations.Tests;

[Trait("Category", "Database")]
public class OnboardingTests
{
    internal sealed record Actor(Guid? UserId, string CorrelationId = "test-correlation") : IRequestActor;

    [Fact]
    public async Task ConcurrentRetriesCreateOneAdministratorContext()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var setup = database.Open();
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = "owner", NormalizedUserName = "OWNER" };
        setup.Users.Add(user);
        await setup.SaveChangesAsync();
        var request = new OnboardingRequest("Family", "Personal", "BRL", "initial-onboarding");
        async Task<OnboardingResult> Run()
        {
            await using var db = database.Open();
            return await new OnboardingService(db, new Actor(user.Id)).OnboardAsync(request);
        }
        var results = await Task.WhenAll(Run(), Run());
        Assert.Equal(results[0], results[1]);
        Assert.Equal(1, await setup.Tenants.CountAsync());
        Assert.Equal(1, await setup.FinancialSpaces.CountAsync());
        Assert.Equal(1, await setup.TenantMembers.CountAsync(x => x.Administrator));
        Assert.Equal(1, await setup.SpaceMembers.CountAsync());
        Assert.Equal(4, await setup.SpaceRoles.CountAsync());
        Assert.Equal(1, await setup.MemberRoles.CountAsync());
        var role = await setup.SpaceRoles.SingleAsync(x => x.Protected);
        Assert.Equal(PermissionCatalog.All.Length, await setup.RolePermissions.CountAsync(x => x.RoleId == role.Id));
        Assert.Equal(1, await setup.AuditRecords.CountAsync(x => x.Action == "onboarding.confirmed"));
        await using var conflictDb = database.Open();
        await Assert.ThrowsAsync<RequestConflictException>(() => new OnboardingService(conflictDb, new Actor(user.Id)).OnboardAsync(request with { TenantName = "Different" }));
    }

    [Fact]
    public async Task InactiveAndUnauthenticatedUsersCannotProvision()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var db = database.Open();
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = "disabled", Active = false };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var request = new OnboardingRequest("Family", "Personal", "BRL", "initial");
        await Assert.ThrowsAsync<AccessDeniedException>(() => new OnboardingService(db, new Actor(null)).OnboardAsync(request));
        await Assert.ThrowsAsync<AccessDeniedException>(() => new OnboardingService(db, new Actor(user.Id)).OnboardAsync(request));
        Assert.Equal(0, await db.Tenants.CountAsync());
    }
}
