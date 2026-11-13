using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Financeiro.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Api.IntegrationTests;

[Trait("Category", "Database")]
public class AccessTests
{
    [Fact]
    public async Task PolicyRejectsReadersAndProtectsFinalAdministrators()
    {
        await using var api = await TestApi.CreateAsync();
        using var owner = api.Browser();
        await TestApi.RegisterAndLoginAsync(owner, "owner@example.test");
        var onboarding = await owner.PostAsJsonAsync("/api/v1/context/onboarding", new { tenantName = "Family", spaceName = "Main", currency = "BRL", requestKey = "initial" });
        onboarding.EnsureSuccessStatusCode();
        var context = await onboarding.Content.ReadFromJsonAsync<JsonElement>();
        var tenantId = context.GetProperty("tenantId").GetGuid();
        var spaceId = context.GetProperty("spaceId").GetGuid();
        Guid ownerId;
        await using (var db = api.OpenMaintenance()) ownerId = (await db.Users.SingleAsync(x => x.Email == "owner@example.test")).Id;
        Assert.Equal(HttpStatusCode.Conflict, (await owner.PostAsJsonAsync($"/api/v1/access/space-members/{ownerId}/deactivate", new { version = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await owner.PostAsJsonAsync($"/api/v1/access/tenant-members/{ownerId}/deactivate", new { version = 1 })).StatusCode);
        using var reader = api.Browser();
        await TestApi.RegisterAndLoginAsync(reader, "reader@example.test");
        await using (var db = api.OpenMaintenance())
        {
            var readerId = (await db.Users.SingleAsync(x => x.Email == "reader@example.test")).Id;
            var role = await db.SpaceRoles.SingleAsync(x => x.Name == "Leitor");
            db.AddRange(new TenantMember { TenantId = tenantId, UserId = readerId },
                new SpaceMember { TenantId = tenantId, SpaceId = spaceId, UserId = readerId },
                new MemberRole { TenantId = tenantId, SpaceId = spaceId, UserId = readerId, RoleId = role.Id });
            await db.SaveChangesAsync();
        }
        (await reader.PostAsJsonAsync("/api/v1/context/select", new { tenantId, spaceId })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Forbidden, (await reader.PostAsJsonAsync($"/api/v1/access/space-members/{ownerId}/deactivate", new { version = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await reader.PostAsJsonAsync($"/api/v1/access/tenant-members/{ownerId}/deactivate", new { version = 1 })).StatusCode);
        await using var verify = api.OpenMaintenance();
        Assert.True((await verify.SpaceMembers.SingleAsync(x => x.UserId == ownerId)).Active);
        Assert.True((await verify.TenantMembers.SingleAsync(x => x.UserId == ownerId)).Active);
    }
}
