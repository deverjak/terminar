using Terminar.SharedKernel;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Tenants.Domain;

public sealed class ExcusalValidityWindow : AggregateRoot<Guid>
{
    public TenantId TenantId { get; private set; } = default!;
    public string Name { get; private set; } = string.Empty;
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    public bool IsDeleted => DeletedAt.HasValue;

    private ExcusalValidityWindow() { }

    public static ExcusalValidityWindow Create(TenantId tenantId, string name, DateOnly startDate, DateOnly endDate)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (endDate <= startDate)
            throw new ArgumentException("EndDate must be after StartDate.", nameof(endDate));

        return new ExcusalValidityWindow
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name.Trim(),
            StartDate = startDate,
            EndDate = endDate,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string? name, DateOnly? startDate, DateOnly? endDate)
    {
        var newStart = startDate ?? StartDate;
        var newEnd = endDate ?? EndDate;
        if (newEnd <= newStart)
            throw new ArgumentException("EndDate must be after StartDate.");
        if (name is not null) Name = name.Trim();
        StartDate = newStart;
        EndDate = newEnd;
    }

    public void SoftDelete()
    {
        if (IsDeleted) return;
        DeletedAt = DateTime.UtcNow;
    }
}
