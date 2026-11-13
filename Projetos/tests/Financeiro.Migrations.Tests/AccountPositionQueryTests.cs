using Financeiro.Application.Accounts;
using Financeiro.Application.Catalogs;
using Financeiro.Domain.Entities;
using Financeiro.Infrastructure.Access;
using Financeiro.Infrastructure.Accounts;
using Financeiro.Infrastructure.Catalogs;
using Financeiro.Infrastructure.Dashboard;
using Financeiro.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Migrations.Tests;

[Trait("Category", "Database")]
public class AccountPositionQueryTests
{
    [Fact]
    public async Task SnapshotsKeepSavingsOverdraftAndCashFlowSeparate()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var setup = database.Open();
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = "owner", SecurityStamp = "stamp" };
        setup.Users.Add(user);
        await setup.SaveChangesAsync();
        var actor = new OnboardingTests.Actor(user.Id);
        var context = await new OnboardingService(setup, actor).OnboardAsync(new("Family", "Main", "BRL", "initial"));
        var institution = new Institution { TenantId = context.TenantId, SpaceId = context.SpaceId, Name = "Bank" };
        var person = new Person { TenantId = context.TenantId, SpaceId = context.SpaceId, Name = "Owner" };
        var account = new BankAccount { TenantId = context.TenantId, SpaceId = context.SpaceId, InstitutionId = institution.Id, OverdraftLimit = 100m };
        var obligation = new Obligation { TenantId = context.TenantId, SpaceId = context.SpaceId, Description = "Bill" };
        var title = new PayableTitle { TenantId = context.TenantId, SpaceId = context.SpaceId, ObligationId = obligation.Id, OriginalAmount = 100m, State = TitleState.Effective, Period = new DateOnly(2026, 11, 1), OccurrenceKey = "bill" };
        var payment = new Payment { TenantId = context.TenantId, SpaceId = context.SpaceId, AccountId = account.Id, PayerId = person.Id, Amount = 5m, PaymentDate = new DateOnly(2026, 11, 9), State = PaymentState.Confirmed, Method = "Transfer", IdempotencyKey = "initial" };
        var now = DateTimeOffset.UtcNow;
        var session = new UserSession { UserId = user.Id, SecurityVersion = user.SecurityStamp!, CreatedAt = now, LastAccessAt = now, ExpiresAt = now.AddHours(1), AbsoluteExpiresAt = now.AddHours(8), ActiveTenantId = context.TenantId, ActiveSpaceId = context.SpaceId };
        setup.AddRange(institution, person, account, obligation, title, payment, session,
            new PaymentAllocation { TenantId = context.TenantId, SpaceId = context.SpaceId, PaymentId = payment.Id, TitleId = title.Id, Amount = 5m });
        await setup.SaveChangesAsync();
        var scope = new FinancialScope();
        await using var db = database.Open(scope);
        Assert.True(await new ScopeResolver(db, scope).ResolveAsync(session.Id, user.Id));
        var permissions = new PermissionEvaluator(db, scope, actor);
        var positions = new AccountPositionService(db, scope, actor, permissions);
        var dashboard = new DashboardQueries(db, permissions);
        var before = await dashboard.CashFlowAsync(new(2026, 11, 1), new(2026, 12, 1), "BRL");
        Assert.Equal("5.00", before.CashOutflow);
        Assert.Null(before.Income);
        var first = await positions.CreateAsync(new(account.Id, new DateOnly(2026, 11, 10), 1000m, 200m, 30m));
        Assert.Equal("1000.00", first.Balance);
        Assert.Equal("200.00", first.SavingsBalance);
        Assert.Equal("70.00", first.AvailableOverdraft);
        var incomplete = await positions.CreateAsync(new(account.Id, new DateOnly(2026, 11, 11), -20m, null, null));
        Assert.Equal("-20.00", incomplete.Balance);
        Assert.Null(incomplete.SavingsBalance);
        Assert.Null(incomplete.AvailableOverdraft);
        var exceeded = await positions.CreateAsync(new(account.Id, new DateOnly(2026, 11, 12), null, null, 130m));
        Assert.Equal("-30.00", exceeded.AvailableOverdraft);
        Assert.Equal(before, await dashboard.CashFlowAsync(new(2026, 11, 1), new(2026, 12, 1), "BRL"));
        Assert.Equal(3, (await positions.ListAsync(new PageRequest(), account.Id)).Total);
        await Assert.ThrowsAsync<CatalogValidationException>(() => positions.CreateAsync(new(account.Id, null, 1m, null, null)));
        var catalogs = new CatalogService(db, scope, actor, permissions);
        await Assert.ThrowsAsync<CatalogValidationException>(() => catalogs.SaveAccountAsync(account.Id, new(institution.Id, null, null, null, null, null, "USD", 100m, Version: 1)));
    }
}
