using Financeiro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Financeiro.Migrations.PostgreSql;

public sealed class PostgreSqlDesignTimeDbContextFactory : IDesignTimeDbContextFactory<FinanceiroDbContext>
{
    public FinanceiroDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("FINANCEIRO_MIGRATION_CONNECTION")
            ?? throw new InvalidOperationException("Set FINANCEIRO_MIGRATION_CONNECTION.");
        var options = new DbContextOptionsBuilder<FinanceiroDbContext>();
        DatabaseConfiguration.Configure(options, "PostgreSql", connection, typeof(PostgreSqlDesignTimeDbContextFactory).Assembly.GetName().Name);
        return new FinanceiroDbContext(options.Options);
    }
}
