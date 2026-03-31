using Microsoft.AspNetCore.Identity;

namespace Terminar.Modules.Identity.Infrastructure.Identity;

public sealed class AppIdentityUser : IdentityUser
{
    public Guid TenantId { get; set; }
    public string TenantSlug { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
