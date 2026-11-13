using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Financeiro.Api.IntegrationTests;

public class HealthTests
{
    [Fact]
    public async Task HealthEndpointReportsProcessAvailability()
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseSetting("Database:Provider", "PostgreSql")
                .UseSetting("ConnectionStrings:Financeiro", "Host=localhost;Database=financeiro"));
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/health");
        response.EnsureSuccessStatusCode();
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
