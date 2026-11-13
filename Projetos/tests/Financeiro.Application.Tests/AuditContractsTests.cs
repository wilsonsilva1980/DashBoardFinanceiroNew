using Financeiro.Application.Access;

namespace Financeiro.Application.Tests;

public class AuditContractsTests
{
    [Fact]
    public void CatalogHasUniqueStableActionsAndLabels()
    {
        Assert.NotEmpty(AuditEventCatalog.All);
        Assert.All(AuditEventCatalog.All, entry =>
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.Key));
            Assert.False(string.IsNullOrWhiteSpace(entry.Value));
            Assert.True(AuditEventCatalog.IsKnown(entry.Key));
            Assert.Equal(entry.Value, AuditEventCatalog.Label(entry.Key));
        });
    }
}
