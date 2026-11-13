using System.Data.Common;
using Financeiro.Infrastructure.Persistence;
using Financeiro.Migrations.PostgreSql;
using Financeiro.Migrations.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Financeiro.Migrations.Tests;

internal sealed class TestDatabase(DbContextOptions<FinanceiroDbContext> options) : IAsyncDisposable
{
    public FinanceiroDbContext Open() => FinanceiroDbContext.CreateForMaintenance(options);
    public FinanceiroDbContext Open(Financeiro.Infrastructure.Access.FinancialScope scope) => new(options, scope);

    public static async Task<TestDatabase> CreateAsync(string? targetMigration = null)
    {
        var provider = Environment.GetEnvironmentVariable("FINANCEIRO_TEST_PROVIDER") ?? throw new InvalidOperationException("Set FINANCEIRO_TEST_PROVIDER.");
        var connection = Environment.GetEnvironmentVariable("FINANCEIRO_TEST_CONNECTION") ?? throw new InvalidOperationException("Set FINANCEIRO_TEST_CONNECTION.");
        var builder = new DbConnectionStringBuilder { ConnectionString = connection };
        builder["Database"] = "financeiro_test_" + Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<FinanceiroDbContext>();
        var assembly = provider == "PostgreSql" ? typeof(PostgreSqlDesignTimeDbContextFactory).Assembly.GetName().Name : typeof(SqlServerDesignTimeDbContextFactory).Assembly.GetName().Name;
        DatabaseConfiguration.Configure(options, provider, builder.ConnectionString, assembly);
        var database = new TestDatabase(options.Options);
        await using var db = database.Open();
        try { await db.GetService<IMigrator>().MigrateAsync(targetMigration); }
        catch { await db.Database.EnsureDeletedAsync(); throw; }
        return database;
    }

    public async ValueTask DisposeAsync()
    {
        await using var db = Open();
        await db.Database.EnsureDeletedAsync();
    }
}
