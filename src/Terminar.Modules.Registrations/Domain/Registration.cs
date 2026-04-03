using Terminar.Modules.Registrations.Domain.Events;
using Terminar.SharedKernel;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Registrations.Domain;

public sealed class Registration : AggregateRoot<Guid>
{
    private readonly List<ParticipantFieldValue> _fieldValues = [];
    public TenantId TenantId { get; private set; } = default!;
    public Guid CourseId { get; private set; }
    public string ParticipantName { get; private set; } = string.Empty;
    public Email ParticipantEmail { get; private set; } = default!;
    public RegistrationSource RegistrationSource { get; private set; }
    public Guid? RegisteredByStaffId { get; private set; }
    public RegistrationStatus Status { get; private set; }
    public IReadOnlyList<ParticipantFieldValue> FieldValues => _fieldValues.AsReadOnly();
    public DateTime RegisteredAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public string? CancellationReason { get; private set; }

    /// <summary>Opaque token used in safe links sent to participants for self-service actions.</summary>
    public Guid SafeLinkToken { get; private set; }

    private Registration() { }

    public static Registration Create(
        TenantId tenantId,
        Guid courseId,
        string participantName,
        Email participantEmail,
        RegistrationSource source,
        Guid? registeredByStaffId)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(participantName);

        var now = DateTime.UtcNow;
        var reg = new Registration
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CourseId = courseId,
            ParticipantName = participantName.Trim(),
            ParticipantEmail = participantEmail,
            RegistrationSource = source,
            RegisteredByStaffId = registeredByStaffId,
            Status = RegistrationStatus.Confirmed,
            RegisteredAt = now,
            SafeLinkToken = Guid.NewGuid()
        };

        reg.RaiseDomainEvent(new RegistrationCreated(
            Guid.NewGuid(), now, reg.Id, reg.CourseId, reg.TenantId,
            reg.ParticipantEmail.Value, reg.ParticipantName, reg.SafeLinkToken));

        return reg;
    }

    public void SetFieldValue(Guid fieldDefinitionId, string? value)
    {
        var existing = _fieldValues.FirstOrDefault(v => v.FieldDefinitionId == fieldDefinitionId);
        if (existing is not null)
        {
            existing.SetValue(value);
        }
        else
        {
            _fieldValues.Add(ParticipantFieldValue.Create(Id, TenantId, fieldDefinitionId, value));
        }
        RaiseDomainEvent(new ParticipantFieldValueUpdated(
            Guid.NewGuid(), DateTime.UtcNow, Id, fieldDefinitionId, value, TenantId));
    }

    public void Cancel(DateTime now, DateTime? lastSessionEndsAt, string? reason = null)
    {
        if (Status == RegistrationStatus.Cancelled)
            throw new ConflictException("Registration is already cancelled.");

        if (lastSessionEndsAt.HasValue && now > lastSessionEndsAt.Value)
            throw new UnprocessableException("Cancellation is no longer allowed after all course sessions have ended.");

        Status = RegistrationStatus.Cancelled;
        CancelledAt = now;
        CancellationReason = reason;

        RaiseDomainEvent(new RegistrationCancelled(
            Guid.NewGuid(), now, Id, CourseId, TenantId,
            ParticipantEmail.Value, ParticipantName));
    }
}
