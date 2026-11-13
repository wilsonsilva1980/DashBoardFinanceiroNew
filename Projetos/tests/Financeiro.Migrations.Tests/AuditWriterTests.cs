using Financeiro.Application.Access;
using Financeiro.Domain.Entities;
using Financeiro.Infrastructure.Access;
using Financeiro.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Migrations.Tests;

[Trait("Category", "Database")]
public class AuditWriterTests
{
    [Fact]
    public async Task RedactsSensitiveDetailsBeforePersisting()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var db = database.Open();
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = "audit-test", NormalizedUserName = "AUDIT-TEST" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var actor = new OnboardingTests.Actor(user.Id, "audit-test");
        var tenant = new Tenant { Name = "Audit tenant", PublicIdentifier = Guid.NewGuid().ToString("N"), CreatedBy = user.Id };
        var space = new FinancialSpace { TenantId = tenant.Id, Name = "Audit space", NormalizedName = "AUDIT SPACE", CreatedBy = user.Id };
        db.AddRange(tenant, space);
        await db.SaveChangesAsync();
        var tenantId = tenant.Id;
        var spaceId = space.Id;
        var writer = new AuditWriter(db, FinancialScope.ForMaintenance(), actor);

        writer.Record("pagamento.confirmado", "Payment", Guid.NewGuid(), new
        {
            password = "do-not-store",
            accessToken = "do-not-store",
            accountNumber = "123456789",
            amount = 10m
        }, tenantId, spaceId);

        Assert.Throws<AuditValidationException>(() =>
            writer.Record("pagamento.confirmado", "Payment", Guid.NewGuid(), new { text = new string('x', 9000) }, tenantId, spaceId));
        await db.SaveChangesAsync();
        var record = await db.AuditRecords.IgnoreQueryFilters().SingleAsync();
        Assert.Contains("[REDACTED]", record.ChangesJson);
        Assert.DoesNotContain("do-not-store", record.ChangesJson);
        Assert.DoesNotContain("123456789", record.ChangesJson);
        Assert.Contains("10", record.ChangesJson);
        Assert.Equal(tenantId, record.TenantId);
        Assert.Equal(spaceId, record.SpaceId);
    }
}
