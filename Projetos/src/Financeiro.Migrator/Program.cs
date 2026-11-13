using Financeiro.Infrastructure.Persistence;
using Financeiro.Migrations.PostgreSql;
using Financeiro.Migrations.SqlServer;
using Microsoft.EntityFrameworkCore;

if (args.Length != 2 || args[0] is not ("PostgreSql" or "SqlServer") || args[1] is not ("Development" or "Test" or "Staging" or "Production"))
{
    Console.Error.WriteLine("Usage: Financeiro.Migrator PostgreSql|SqlServer Development|Test|Staging|Production");
    return 2;
}
var connection = Environment.GetEnvironmentVariable("FINANCEIRO_MIGRATION_CONNECTION");
if (string.IsNullOrWhiteSpace(connection))
{
    Console.Error.WriteLine("Set FINANCEIRO_MIGRATION_CONNECTION.");
    return 2;
}
var assembly = args[0] == "PostgreSql"
    ? typeof(PostgreSqlDesignTimeDbContextFactory).Assembly.GetName().Name
    : typeof(SqlServerDesignTimeDbContextFactory).Assembly.GetName().Name;
var options = new DbContextOptionsBuilder<FinanceiroDbContext>();
DatabaseConfiguration.Configure(options, args[0], connection, assembly);
await using var context = new FinanceiroDbContext(options.Options);
await context.Database.MigrateAsync();
Console.WriteLine($"Migrations applied: provider={args[0]}, environment={args[1]}.");
return 0;
