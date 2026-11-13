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
        var connection = configuration.GetConnectionString("Financeiro");
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException("Configure ConnectionStrings:Financeiro.");
        if (provider is not ("PostgreSql" or "SqlServer"))
            throw new InvalidOperationException("Database:Provider must be PostgreSql or SqlServer.");
        services.AddScoped<FinancialScope>();
        services.AddScoped<ScopeResolver>();
        if (provider == "PostgreSql") services.AddScoped<ITituloLockService, PostgreSqlTitleLockService>();
        else services.AddScoped<ITituloLockService, SqlServerTitleLockService>();
        services.AddDbContext<FinanceiroDbContext>(options => Configure(options, provider, connection));
        return services;
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
