using Financeiro.Domain.Entities;
using Financeiro.Infrastructure.Identity;
using Financeiro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Financeiro.Infrastructure.IntegrationTests;

public class ModelContractTests
{
    [Theory]
    [InlineData("PostgreSql")]
    [InlineData("SqlServer")]
    public void ModelEnforcesRelationalScopeAndFinancialConventions(string provider)
    {
        var options = new DbContextOptionsBuilder<FinanceiroDbContext>();
        DatabaseConfiguration.Configure(options, provider, "Server=localhost;Database=financeiro");
        using var context = new FinanceiroDbContext(options.Options);
        var model = context.GetService<IDesignTimeModel>().Model;
        var scoped = model.GetEntityTypes().Where(x => typeof(ScopedEntity).IsAssignableFrom(x.ClrType)).ToArray();
        Assert.True(scoped.Length >= 30);
        foreach (var entity in scoped)
        {
            Assert.Contains(entity.GetKeys(), k => k.Properties.Select(p => p.Name).SequenceEqual(["TenantId", "SpaceId", "Id"]));
            Assert.True(entity.FindProperty("Version")!.IsConcurrencyToken);
            foreach (var foreignKey in entity.GetForeignKeys().Where(f => typeof(ScopedEntity).IsAssignableFrom(f.PrincipalEntityType.ClrType)))
            {
                Assert.Equal("TenantId", foreignKey.Properties[0].Name);
                Assert.Equal("SpaceId", foreignKey.Properties[1].Name);
                Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
            }
        }
        var amount = model.FindEntityType(typeof(PayableTitle))!.FindProperty(nameof(PayableTitle.OriginalAmount))!;
        Assert.Equal(19, amount.GetPrecision());
        Assert.Equal(2, amount.GetScale());
        Assert.Equal(typeof(string), model.FindEntityType(typeof(BankAccount))!.FindProperty(nameof(BankAccount.Number))!.ClrType);
        Assert.Contains(model.FindEntityType(typeof(ApplicationUser))!.GetIndexes(), x => x.IsUnique && x.Properties.Any(p => p.Name == "NormalizedEmail"));
        var ddl = context.Database.GenerateCreateScript();
        Assert.Contains("CK_PayableTitle_period", ddl);
        Assert.Contains("FK_titulos", ddl);
        Assert.Contains(provider == "PostgreSql" ? "EXTRACT(DAY" : "DATEPART(day", ddl);
        Assert.DoesNotContain("Discriminator", ddl);
    }
}
