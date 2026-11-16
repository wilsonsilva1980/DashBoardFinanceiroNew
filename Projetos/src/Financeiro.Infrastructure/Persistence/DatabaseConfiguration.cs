using Microsoft.EntityFrameworkCore;
using Financeiro.Infrastructure.Access;
using Financeiro.Infrastructure.Payments;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Financeiro.Infrastructure.Persistence;

public static class DatabaseConfiguration
{
    public static IServiceCollection AddFinancialDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"];
        if (provider is not ("PostgreSql" or "SqlServer"))
            throw new InvalidOperationException("Database:Provider must be PostgreSql or SqlServer.");
        var connection = ResolveConnectionString(configuration, provider);
        services.AddScoped<FinancialScope>();
        services.AddScoped<ScopeResolver>();
        if (provider == "PostgreSql") services.AddScoped<ITituloLockService, PostgreSqlTitleLockService>();
        else services.AddScoped<ITituloLockService, SqlServerTitleLockService>();
        services.AddDbContext<FinanceiroDbContext>(options => Configure(options, provider, connection));
        return services;
    }

    private static string ResolveConnectionString(IConfiguration configuration, string provider)
    {
        // ConnectionStrings:Financeiro overrides the provider-specific entry (tests and explicit overrides).
        var connection = configuration.GetConnectionString("Financeiro");
        if (string.IsNullOrWhiteSpace(connection)) connection = configuration.GetConnectionString(provider);
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException(
                $"Configure ConnectionStrings:{provider} in appsettings.json/appsettings.Development.json, or set the ConnectionStrings__{provider} environment variable (ConnectionStrings:Financeiro keeps overriding both). See Projetos/README.md.");
        return connection;
    }

    public static void Configure(DbContextOptionsBuilder options, string provider, string connection, string? migrationsAssembly = null)
    {
        switch (provider)
        {
            case "PostgreSql": options.UseNpgsql(connection, x => x.MigrationsAssembly(migrationsAssembly)); break;
            case "SqlServer": options.UseSqlServer(connection, x => x.MigrationsAssembly(migrationsAssembly)); break;
            default: throw new ArgumentException("Unsupported database provider.", nameof(provider));
        }
    }
}
