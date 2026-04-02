using Terminar.Modules.Registrations.Domain.Events;
using Terminar.SharedKernel;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Registrations.Domain;

public sealed class Excusal : AggregateRoot<Guid>
{
    public TenantId TenantId { get; private set; } = default!;
    public Guid RegistrationId { get; private set; }
    public Guid CourseId { get; private set; }
    public Guid SessionId { get; private set; }
    public string ParticipantEmail { get; private set; } = string.Empty;
    public string ParticipantName { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public ExcusalStatus Status { get; private set; }
    public Guid? ExcusalCreditId { get; private set; }

    private Excusal() { }

    public static Excusal Create(
        TenantId tenantId,
        Guid registrationId,
        Guid courseId,
        Guid sessionId,
        string participantEmail,
        string participantName)
    {
        var now = DateTime.UtcNow;
        var excusal = new Excusal
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RegistrationId = registrationId,
            CourseId = courseId,
            SessionId = sessionId,
            ParticipantEmail = participantEmail,
            ParticipantName = participantName,
            CreatedAt = now,
            Status = ExcusalStatus.Recorded
        };

        excusal.RaiseDomainEvent(new ExcusalCreated(
            Guid.NewGuid(), now, excusal.Id, registrationId, courseId, sessionId,
            tenantId, participantEmail, participantName));

        return excusal;
    }

    public void MarkCreditIssued(Guid creditId)
    {
        Status = ExcusalStatus.CreditIssued;
        ExcusalCreditId = creditId;
    }
}
