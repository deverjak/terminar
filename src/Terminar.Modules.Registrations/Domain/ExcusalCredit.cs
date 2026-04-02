using Terminar.Modules.Registrations.Domain.Events;
using Terminar.SharedKernel;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Registrations.Domain;

public sealed class ExcusalCreditAuditEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ExcusalCreditId { get; set; }
    public Guid ActorStaffId { get; init; }
    public ExcusalCreditActionType ActionType { get; init; }
    public string FieldChanged { get; init; } = string.Empty;
    public string PreviousValue { get; init; } = string.Empty;
    public string NewValue { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public sealed class ExcusalCredit : AggregateRoot<Guid>
{
    private List<ExcusalCreditAuditEntry> _auditEntries = [];

    public TenantId TenantId { get; private set; } = default!;
    public string ParticipantEmail { get; private set; } = string.Empty;
    public string ParticipantName { get; private set; } = string.Empty;
    public Guid SourceExcusalId { get; private set; }
    public Guid SourceCourseId { get; private set; }
    public Guid SourceSessionId { get; private set; }
    public List<string> Tags { get; private set; } = [];
    public List<Guid> ValidWindowIds { get; private set; } = [];
    public ExcusalCreditStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? RedeemedAt { get; private set; }
    public Guid? RedeemedCourseId { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public IReadOnlyList<ExcusalCreditAuditEntry> AuditEntries => _auditEntries.AsReadOnly();

    public bool IsActive => Status == ExcusalCreditStatus.Active && DeletedAt is null;

    private ExcusalCredit() { }

    public static ExcusalCredit Issue(
        TenantId tenantId,
        string participantEmail,
        string participantName,
        Guid sourceExcusalId,
        Guid sourceCourseId,
        Guid sourceSessionId,
        List<string> tags,
        List<Guid> validWindowIds)
    {
        if (tags.Count == 0)
            throw new ArgumentException("Tags must not be empty.", nameof(tags));
        if (validWindowIds.Count == 0)
            throw new ArgumentException("ValidWindowIds must not be empty.", nameof(validWindowIds));

        var now = DateTime.UtcNow;
        var credit = new ExcusalCredit
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ParticipantEmail = participantEmail,
            ParticipantName = participantName,
            SourceExcusalId = sourceExcusalId,
            SourceCourseId = sourceCourseId,
            SourceSessionId = sourceSessionId,
            Tags = [.. tags],
            ValidWindowIds = [.. validWindowIds],
            Status = ExcusalCreditStatus.Active,
            CreatedAt = now
        };

        credit.RaiseDomainEvent(new ExcusalCreditIssued(Guid.NewGuid(), now, credit.Id, tenantId, participantEmail, sourceExcusalId));
        return credit;
    }

    public void Redeem(Guid courseId)
    {
        EnsureActive();
        Status = ExcusalCreditStatus.Redeemed;
        RedeemedAt = DateTime.UtcNow;
        RedeemedCourseId = courseId;
        RaiseDomainEvent(new ExcusalCreditRedeemed(Guid.NewGuid(), RedeemedAt.Value, Id, TenantId, courseId, ParticipantEmail));
    }

    public void Extend(List<Guid> additionalWindowIds, Guid actorStaffId)
    {
        EnsureActive();
        if (additionalWindowIds.Count == 0)
            throw new ArgumentException("Must provide at least one additional window.", nameof(additionalWindowIds));

        var previous = System.Text.Json.JsonSerializer.Serialize(ValidWindowIds);
        ValidWindowIds.AddRange(additionalWindowIds);
        var newValue = System.Text.Json.JsonSerializer.Serialize(ValidWindowIds);

        _auditEntries.Add(new ExcusalCreditAuditEntry
        {
            ExcusalCreditId = Id,
            ActorStaffId = actorStaffId,
            ActionType = ExcusalCreditActionType.Extend,
            FieldChanged = nameof(ValidWindowIds),
            PreviousValue = previous,
            NewValue = newValue
        });
    }

    public void ReTag(List<string> newTags, Guid actorStaffId)
    {
        EnsureActive();
        if (newTags.Count == 0)
            throw new ArgumentException("Tags must not be empty.", nameof(newTags));

        var previous = System.Text.Json.JsonSerializer.Serialize(Tags);
        Tags = [.. newTags];
        var newValue = System.Text.Json.JsonSerializer.Serialize(Tags);

        _auditEntries.Add(new ExcusalCreditAuditEntry
        {
            ExcusalCreditId = Id,
            ActorStaffId = actorStaffId,
            ActionType = ExcusalCreditActionType.ReTag,
            FieldChanged = nameof(Tags),
            PreviousValue = previous,
            NewValue = newValue
        });
    }

    public void SoftDelete(Guid actorStaffId)
    {
        EnsureActive();
        DeletedAt = DateTime.UtcNow;
        Status = ExcusalCreditStatus.Cancelled;
        RaiseDomainEvent(new ExcusalCreditCancelled(Guid.NewGuid(), DeletedAt.Value, Id, TenantId));

        _auditEntries.Add(new ExcusalCreditAuditEntry
        {
            ExcusalCreditId = Id,
            ActorStaffId = actorStaffId,
            ActionType = ExcusalCreditActionType.SoftDelete,
            FieldChanged = nameof(Status),
            PreviousValue = ExcusalCreditStatus.Active.ToString(),
            NewValue = ExcusalCreditStatus.Cancelled.ToString()
        });
    }

    private void EnsureActive()
    {
        if (!IsActive)
            throw new UnprocessableException("Excusal credit is not active.");
    }
}
