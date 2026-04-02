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
    public DateTime CreatedAt { get; private set; }
    public TenantExcusalSettings ExcusalSettings { get; private set; } = TenantExcusalSettings.Default();

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
            CreatedAt = DateTime.UtcNow
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

    public void UpdateExcusalSettings(
        bool? creditGenerationEnabled,
        int? forwardWindowCount,
        int? unenrollmentDeadlineDays,
        int? excusalDeadlineHours)
    {
        if (creditGenerationEnabled.HasValue)
            ExcusalSettings.CreditGenerationEnabled = creditGenerationEnabled.Value;
        if (forwardWindowCount.HasValue)
        {
            if (forwardWindowCount.Value < 1)
                throw new ArgumentException("ForwardWindowCount must be at least 1.");
            ExcusalSettings.ForwardWindowCount = forwardWindowCount.Value;
        }
        if (unenrollmentDeadlineDays.HasValue)
        {
            if (unenrollmentDeadlineDays.Value < 0)
                throw new ArgumentException("UnenrollmentDeadlineDays must be >= 0.");
            ExcusalSettings.UnenrollmentDeadlineDays = unenrollmentDeadlineDays.Value;
        }
        if (excusalDeadlineHours.HasValue)
        {
            if (excusalDeadlineHours.Value < 0)
                throw new ArgumentException("ExcusalDeadlineHours must be >= 0.");
            ExcusalSettings.ExcusalDeadlineHours = excusalDeadlineHours.Value;
        }
    }

    private static bool IsValidSlug(string slug) =>
        !string.IsNullOrWhiteSpace(slug) &&
        slug.All(c => char.IsLower(c) || char.IsDigit(c) || c == '-');
}
