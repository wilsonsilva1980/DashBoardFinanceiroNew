using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Financeiro.Api.IntegrationTests;

public class OpenApiTests
{
    [Fact]
    public async Task DevelopmentDocumentsCookieAndCsrfContractsAndServesSwagger()
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.UseEnvironment("Development")
            .UseSetting("Database:Provider", "PostgreSql").UseSetting("ConnectionStrings:Financeiro", "Host=localhost;Database=financeiro"));
        using var client = factory.CreateClient();
        using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
        var schemes = document.RootElement.GetProperty("components").GetProperty("securitySchemes");
        Assert.Equal("cookie", schemes.GetProperty("cookieAuth").GetProperty("in").GetString());
        Assert.Equal("X-CSRF-TOKEN", schemes.GetProperty("csrf").GetProperty("name").GetString());
        var examples = document.RootElement.GetProperty("components").GetProperty("examples");
        Assert.True(examples.TryGetProperty("pageResult", out _));
        Assert.True(examples.TryGetProperty("problemDetails", out _));
        var logout = document.RootElement.GetProperty("paths").GetProperty("/api/v1/auth/logout").GetProperty("post").GetProperty("security")[0];
        Assert.True(logout.TryGetProperty("cookieAuth", out _));
        Assert.True(logout.TryGetProperty("csrf", out _));
        Assert.Contains("swagger-ui-bundle.js", await client.GetStringAsync("/swagger/index.html"));
    }

    [Fact]
    public async Task DocumentContainsVersionedContractWithPaginationAndDashboardOperations()
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.UseEnvironment("Development")
            .UseSetting("Database:Provider", "PostgreSql").UseSetting("ConnectionStrings:Financeiro", "Host=localhost;Database=financeiro"));
        using var client = factory.CreateClient();
        using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
        var paths = document.RootElement.GetProperty("paths");

        foreach (var path in new[] { "/api/v1/contas", "/api/v1/titulos", "/api/v1/pagamentos", "/api/v1/auditoria", "/api/v1/dashboard/resumo" })
            Assert.True(paths.TryGetProperty(path, out _), $"Missing versioned operation {path}");

        var list = paths.GetProperty("/api/v1/contas").GetProperty("get");
        Assert.Contains(list.GetProperty("parameters").EnumerateArray(), parameter => parameter.GetProperty("name").GetString() == "page");
        Assert.Contains(list.GetProperty("parameters").EnumerateArray(), parameter => parameter.GetProperty("name").GetString() == "pageSize");
    }
}
