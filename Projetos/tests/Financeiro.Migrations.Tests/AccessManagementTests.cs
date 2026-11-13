using Financeiro.Application.Access;
using Financeiro.Domain.Entities;
using Financeiro.Infrastructure.Access;
using Financeiro.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Migrations.Tests;

[Trait("Category", "Database")]
public class AccessManagementTests
{
    [Fact]
    public async Task ConcurrentRevocationsPreserveFinalSpaceAndTenantAdministrators()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var setup = database.Open();
        var first = new ApplicationUser { Id = Guid.NewGuid(), UserName = "first", SecurityStamp = "first-stamp" };
        var second = new ApplicationUser { Id = Guid.NewGuid(), UserName = "second", SecurityStamp = "second-stamp" };
        setup.Users.AddRange(first, second);
        await setup.SaveChangesAsync();
        var result = await new OnboardingService(setup, new OnboardingTests.Actor(first.Id)).OnboardAsync(new("Tenant", "Space", "BRL", "initial"));
        var admin = await setup.SpaceRoles.SingleAsync(x => x.Protected);
        var now = DateTimeOffset.UtcNow;
        UserSession Session(ApplicationUser user) => new() { UserId = user.Id, CreatedAt = now, LastAccessAt = now, ExpiresAt = now.AddHours(1), AbsoluteExpiresAt = now.AddHours(8), SecurityVersion = user.SecurityStamp!, ActiveTenantId = result.TenantId, ActiveSpaceId = result.SpaceId };
        var firstSession = Session(first);
        var secondSession = Session(second);
        setup.AddRange(firstSession, secondSession,
            new TenantMember { TenantId = result.TenantId, UserId = second.Id, Administrator = true },
            new SpaceMember { TenantId = result.TenantId, SpaceId = result.SpaceId, UserId = second.Id },
            new MemberRole { TenantId = result.TenantId, SpaceId = result.SpaceId, UserId = second.Id, RoleId = admin.Id });
        await setup.SaveChangesAsync();
        async Task<bool> RevokeSelf(ApplicationUser user, UserSession session)
        {
            var scope = new FinancialScope();
            await using var db = database.Open(scope);
            Assert.True(await new ScopeResolver(db, scope).ResolveAsync(session.Id, user.Id));
            var actor = new OnboardingTests.Actor(user.Id);
            var service = new AccessManagementService(db, scope, actor, new PermissionEvaluator(db, scope, actor));
            try { await service.DeactivateSpaceMemberAsync(user.Id, 1); return true; }
            catch (RequestConflictException) { return false; }
        }
        var outcomes = await Task.WhenAll(RevokeSelf(first, firstSession), RevokeSelf(second, secondSession));
        Assert.Single(outcomes, x => x);
        setup.ChangeTracker.Clear();
        var remaining = await setup.SpaceMembers.SingleAsync(x => x.Active);
        var remainingSession = remaining.UserId == first.Id ? firstSession : secondSession;
        var scope = new FinancialScope();
        await using var db = database.Open(scope);
        Assert.True(await new ScopeResolver(db, scope).ResolveAsync(remainingSession.Id, remaining.UserId));
        var actor = new OnboardingTests.Actor(remaining.UserId);
        var service = new AccessManagementService(db, scope, actor, new PermissionEvaluator(db, scope, actor));
        await Assert.ThrowsAsync<RequestConflictException>(() => service.DeactivateSpaceMemberAsync(remaining.UserId, remaining.Version));
        var otherId = remaining.UserId == first.Id ? second.Id : first.Id;
        await service.DeactivateTenantMemberAsync(otherId, 1);
        await Assert.ThrowsAsync<RequestConflictException>(() => service.DeactivateTenantMemberAsync(remaining.UserId, 1));
        Assert.Equal(1, await setup.TenantMembers.CountAsync(x => x.Active && x.Administrator));
    }
}
