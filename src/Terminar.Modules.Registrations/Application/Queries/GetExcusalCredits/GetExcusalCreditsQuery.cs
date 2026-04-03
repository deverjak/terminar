using MediatR;
using Terminar.Modules.Registrations.Domain;

namespace Terminar.Modules.Registrations.Application.Queries.GetExcusalCredits;

public sealed record GetExcusalCreditsQuery(
    Guid TenantId,
    ExcusalCreditStatus? Status,
    string? ParticipantEmail,
    int Page,
    int PageSize) : IRequest<GetExcusalCreditsResult>;

public sealed record GetExcusalCreditsResult(
    List<ExcusalCreditDto> Items,
    int Total,
    int Page,
    int PageSize);

public sealed record ExcusalCreditDto(
    Guid CreditId,
    string ParticipantEmail,
    string ParticipantName,
    Guid SourceCourseId,
    Guid SourceSessionId,
    List<string> Tags,
    List<Guid> ValidWindowIds,
    string Status,
    DateTime CreatedAt,
    DateTime? DeletedAt,
    List<AuditEntryDto> AuditEntries);

public sealed record AuditEntryDto(
    Guid ActorStaffId,
    string ActionType,
    string FieldChanged,
    string PreviousValue,
    string NewValue,
    DateTime Timestamp);
