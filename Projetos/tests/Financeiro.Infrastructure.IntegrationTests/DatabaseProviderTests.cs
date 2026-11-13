using Financeiro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Financeiro.Infrastructure.IntegrationTests;

public class DatabaseProviderTests
{
    [Theory]
    [InlineData("PostgreSql", "Npgsql.EntityFrameworkCore.PostgreSQL")]
    [InlineData("SqlServer", "Microsoft.EntityFrameworkCore.SqlServer")]
    public void SelectsExactlyOneProvider(string provider, string expected)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:Provider"] = provider,
            ["ConnectionStrings:Financeiro"] = "Server=localhost;Database=financeiro"
        }).Build();
        var services = new ServiceCollection().AddFinancialDatabase(configuration).BuildServiceProvider();
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        Assert.Equal(expected, context.Database.ProviderName);
    }

    [Fact]
    public void RejectsUnknownProviderAtStartup()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "Other",
            ["ConnectionStrings:Financeiro"] = "Server=localhost"
        }).Build();
        Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddFinancialDatabase(configuration));
    }

    [Fact]
    [Trait("Category", "Database")]
    public async Task ConfiguredDatabaseAcceptsConnections()
    {
        var provider = Environment.GetEnvironmentVariable("FINANCEIRO_TEST_PROVIDER")
            ?? throw new InvalidOperationException("Set FINANCEIRO_TEST_PROVIDER for database integration tests.");
        var connection = Environment.GetEnvironmentVariable("FINANCEIRO_TEST_CONNECTION")
            ?? throw new InvalidOperationException("Set FINANCEIRO_TEST_CONNECTION for database integration tests.");
        var options = new DbContextOptionsBuilder<FinanceiroDbContext>();
        DatabaseConfiguration.Configure(options, provider, connection);
        await using var context = new FinanceiroDbContext(options.Options);
        Assert.True(await context.Database.CanConnectAsync());
    }
}
