using Terminar.Modules.Tenants.Domain.Events;
using Terminar.SharedKernel;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Tenants.Domain;

public sealed class Tenant : AggregateRoot<TenantId>
{
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string DefaultLanguageCode { get; private set; } = string.Empty;
    public TenantStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Tenant() { }

    public static Tenant Create(string name, string slug, string defaultLanguageCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultLanguageCode);

        if (!IsValidSlug(slug))
            throw new ArgumentException("Slug must contain only lowercase letters, digits, and hyphens.", nameof(slug));

        var tenant = new Tenant
        {
            Id = TenantId.New(),
            Name = name.Trim(),
            Slug = slug.Trim().ToLowerInvariant(),
            DefaultLanguageCode = defaultLanguageCode.Trim().ToLowerInvariant(),
            Status = TenantStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };

        tenant.RaiseDomainEvent(new TenantCreated(
            Guid.NewGuid(),
            tenant.CreatedAt,
            tenant.Id,
            tenant.Name,
            tenant.Slug));

        return tenant;
    }

    public void Suspend()
    {
        if (Status == TenantStatus.Suspended)
            return;
        Status = TenantStatus.Suspended;
    }

    public void Reactivate()
    {
        if (Status == TenantStatus.Active)
            return;
        Status = TenantStatus.Active;
    }

    private static bool IsValidSlug(string slug) =>
        !string.IsNullOrWhiteSpace(slug) &&
        slug.All(c => char.IsLower(c) || char.IsDigit(c) || c == '-');
}
