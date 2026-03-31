using Terminar.Modules.Identity.Domain.Events;
using Terminar.SharedKernel;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Identity.Domain;

public sealed class StaffUser : AggregateRoot<Guid>
{
    public TenantId TenantId { get; private set; } = default!;
    public string Username { get; private set; } = string.Empty;
    public Email Email { get; private set; } = default!;
    public StaffRole Role { get; private set; }
    public StaffUserStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    private StaffUser() { }

    public static StaffUser Create(TenantId tenantId, string username, Email email, StaffRole role)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentNullException.ThrowIfNull(email);

        var user = new StaffUser
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Username = username.Trim(),
            Email = email,
            Role = role,
            Status = StaffUserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        user.RaiseDomainEvent(new StaffUserCreated(
            Guid.NewGuid(),
            user.CreatedAt,
            user.Id,
            user.TenantId,
            user.Username));

        return user;
    }

    public void Deactivate()
    {
        if (Status == StaffUserStatus.Deactivated)
            return;
        Status = StaffUserStatus.Deactivated;
    }

    public void RecordLogin() => LastLoginAt = DateTime.UtcNow;

    public bool IsActive() => Status == StaffUserStatus.Active;
}
