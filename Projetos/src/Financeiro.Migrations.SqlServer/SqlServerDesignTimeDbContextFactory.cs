using Financeiro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Financeiro.Migrations.SqlServer;

public sealed class SqlServerDesignTimeDbContextFactory : IDesignTimeDbContextFactory<FinanceiroDbContext>
{
    public FinanceiroDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("FINANCEIRO_MIGRATION_CONNECTION")
            ?? throw new InvalidOperationException("Set FINANCEIRO_MIGRATION_CONNECTION.");
        var options = new DbContextOptionsBuilder<FinanceiroDbContext>();
        DatabaseConfiguration.Configure(options, "SqlServer", connection, typeof(SqlServerDesignTimeDbContextFactory).Assembly.GetName().Name);
        return new FinanceiroDbContext(options.Options);
    }
}
