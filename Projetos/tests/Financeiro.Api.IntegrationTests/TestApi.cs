using System.Data.Common;
using System.Net.Http.Json;
using System.Text.Json;
using Financeiro.Infrastructure.Persistence;
using Financeiro.Migrations.PostgreSql;
using Financeiro.Migrations.SqlServer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Api.IntegrationTests;

internal sealed class TestApi(string provider, string connection, DbContextOptions<FinanceiroDbContext> options) : WebApplicationFactory<Program>
{
    private readonly CaptureException _exceptions = new();
    public Exception? LastException => _exceptions.Last;
    private sealed class CaptureException : IExceptionHandler
    {
        public Exception? Last { get; private set; }
        public ValueTask<bool> TryHandleAsync(Microsoft.AspNetCore.Http.HttpContext context, Exception exception, CancellationToken cancellationToken)
        {
            Last = exception;
            return ValueTask.FromResult(false);
        }
    }

    public FinanceiroDbContext OpenMaintenance() => FinanceiroDbContext.CreateForMaintenance(options);

    public static async Task<TestApi> CreateAsync()
    {
        var provider = Environment.GetEnvironmentVariable("FINANCEIRO_TEST_PROVIDER") ?? throw new InvalidOperationException("Set FINANCEIRO_TEST_PROVIDER.");
        var connection = Environment.GetEnvironmentVariable("FINANCEIRO_TEST_CONNECTION") ?? throw new InvalidOperationException("Set FINANCEIRO_TEST_CONNECTION.");
        var builder = new DbConnectionStringBuilder { ConnectionString = connection };
        builder["Database"] = "financeiro_api_test_" + Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<FinanceiroDbContext>();
        var assembly = provider == "PostgreSql" ? typeof(PostgreSqlDesignTimeDbContextFactory).Assembly.GetName().Name : typeof(SqlServerDesignTimeDbContextFactory).Assembly.GetName().Name;
        DatabaseConfiguration.Configure(options, provider, builder.ConnectionString, assembly);
        var api = new TestApi(provider, builder.ConnectionString, options.Options);
        await using var db = api.OpenMaintenance();
        try { await db.Database.MigrateAsync(); }
        catch { await db.Database.EnsureDeletedAsync(); throw; }
        return api;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Development")
        .UseSetting("Database:Provider", provider).UseSetting("ConnectionStrings:Financeiro", connection)
        .ConfigureServices(services => services.Insert(0, ServiceDescriptor.Singleton<IExceptionHandler>(_exceptions)));

    public HttpClient Browser() => CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

    public static async Task RefreshCsrfAsync(HttpClient client)
    {
        var response = await client.GetFromJsonAsync<JsonElement>("/api/v1/auth/csrf");
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", response.GetProperty("token").GetString());
    }

    public static async Task<string> RegisterAndLoginAsync(HttpClient client, string email)
    {
        await RefreshCsrfAsync(client);
        var registration = await client.PostAsJsonAsync("/api/v1/auth/register", new { email, password = "TestPassword123!", displayName = "Test user" });
        registration.EnsureSuccessStatusCode();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "TestPassword123!" });
        login.EnsureSuccessStatusCode();
        var cookie = login.Headers.GetValues("Set-Cookie").Single(x => x.StartsWith("__Host-financeiro-auth="));
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        await RefreshCsrfAsync(client);
        return cookie.Split(';')[0];
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await using var db = OpenMaintenance();
        await db.Database.EnsureDeletedAsync();
    }
}
