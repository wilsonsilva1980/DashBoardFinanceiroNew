namespace Financeiro.Domain.Entities;

public sealed class Tenant : Entity
{
    public string Name { get; set; } = string.Empty;
    public string PublicIdentifier { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
}
