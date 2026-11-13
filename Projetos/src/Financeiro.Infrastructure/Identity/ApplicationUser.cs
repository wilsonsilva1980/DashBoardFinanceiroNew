using Microsoft.AspNetCore.Identity;

namespace Financeiro.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
    public bool Active { get; set; } = true;
    public bool MustChangePassword { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastLoginAt { get; set; }
}
