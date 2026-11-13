namespace Financeiro.Domain.Entities;

public sealed class SpaceRole : ScopedEntity
{
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Protected { get; set; }
    public bool Active { get; set; } = true;
}
