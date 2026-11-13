using System.Data.Common;
using Financeiro.Domain.Entities;
using Financeiro.Infrastructure.Identity;
using Financeiro.Infrastructure.Persistence;
using Financeiro.Migrations.PostgreSql;
using Financeiro.Migrations.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Financeiro.Migrations.Tests;

[Trait("Category", "Database")]
public class MigrationTests
{
    [Fact]
    public async Task FreshMigrationIsRepeatableAndEnforcesScopeAndMoney()
    {
        var provider = Environment.GetEnvironmentVariable("FINANCEIRO_TEST_PROVIDER")
            ?? throw new InvalidOperationException("Set FINANCEIRO_TEST_PROVIDER.");
        var connection = Environment.GetEnvironmentVariable("FINANCEIRO_TEST_CONNECTION")
            ?? throw new InvalidOperationException("Set FINANCEIRO_TEST_CONNECTION.");
        var connectionBuilder = new DbConnectionStringBuilder { ConnectionString = connection };
        connectionBuilder["Database"] = "financeiro_test_" + Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<FinanceiroDbContext>();
        var assembly = provider == "PostgreSql" ? typeof(PostgreSqlDesignTimeDbContextFactory).Assembly.GetName().Name : typeof(SqlServerDesignTimeDbContextFactory).Assembly.GetName().Name;
        DatabaseConfiguration.Configure(options, provider, connectionBuilder.ConnectionString, assembly);
        await using var context = FinanceiroDbContext.CreateForMaintenance(options.Options);
        try
        {
            var available = context.Database.GetMigrations().ToArray();
            Assert.True(available.Length >= 2);
            await context.GetService<IMigrator>().MigrateAsync(available[0]);
            Assert.Single(await context.Database.GetAppliedMigrationsAsync());
            await context.Database.MigrateAsync();
            var applied = (await context.Database.GetAppliedMigrationsAsync()).ToArray();
            Assert.NotEmpty(applied);
            await context.Database.MigrateAsync();
            Assert.Equal(applied, await context.Database.GetAppliedMigrationsAsync());
            Assert.False(context.Database.HasPendingModelChanges());
            var tenant = new Tenant { Name = "Migration test", PublicIdentifier = Guid.NewGuid().ToString("N") };
            var space = new FinancialSpace { TenantId = tenant.Id, Name = "One", NormalizedName = "ONE" };
            var otherSpace = new FinancialSpace { TenantId = tenant.Id, Name = "Two", NormalizedName = "TWO" };
            var category = new Category { TenantId = tenant.Id, SpaceId = otherSpace.Id, Name = "Foreign" };
            var obligation = new Obligation { TenantId = tenant.Id, SpaceId = space.Id, Description = "Valid", PurchaseDate = null };
            context.AddRange(tenant, space, otherSpace, category, obligation);
            await context.SaveChangesAsync();
            var invalid = new Obligation { TenantId = tenant.Id, SpaceId = space.Id, Description = "Cross-space", CategoryId = category.Id };
            context.Add(invalid);
            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
            context.ChangeTracker.Clear();
            var title = new PayableTitle { TenantId = tenant.Id, SpaceId = space.Id, ObligationId = obligation.Id, Period = new DateOnly(2026, 11, 1), OriginalAmount = -1m, OccurrenceKey = "invalid" };
            context.Add(title);
            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
            context.ChangeTracker.Clear();
            title.OriginalAmount = 123.45m;
            title.Id = Guid.NewGuid();
            title.OccurrenceKey = "valid";
            context.Add(title);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            var stored = await context.PayableTitles.SingleAsync();
            Assert.Equal(123.45m, stored.OriginalAmount);
            Assert.Null(stored.DueDate);
            Assert.Null((await context.Obligations.SingleAsync()).PurchaseDate);
            stored.OriginalAmount = 140m;
            await context.SaveChangesAsync();
            Assert.Equal(2, stored.Version);
            await using var competing = FinanceiroDbContext.CreateForMaintenance(options.Options);
            var stale = await competing.PayableTitles.SingleAsync();
            stored.OriginalAmount = 150m;
            await context.SaveChangesAsync();
            stale.OriginalAmount = 160m;
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => competing.SaveChangesAsync());
            context.ChangeTracker.Clear();
            var parent = await context.Obligations.SingleAsync();
            context.Remove(parent);
            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        }
        finally
        {
            // Only the randomly named database created above is ever removed.
            await context.Database.EnsureDeletedAsync();
        }
    }
}
