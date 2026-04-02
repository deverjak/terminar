using MediatR;

namespace Terminar.Modules.Registrations.Application.Queries.GetParticipantCourseView;

public sealed record GetParticipantCourseViewQuery(Guid SafeLinkToken, Guid TenantId)
    : IRequest<ParticipantCourseViewDto?>;

public sealed record ParticipantCourseViewDto(
    Guid EnrollmentId,
    Guid CourseId,
    string CourseTitle,
    string CourseStatus,
    string ParticipantName,
    string EnrollmentStatus,
    DateTime? UnenrollmentDeadlineAt,
    bool CanUnenroll,
    IReadOnlyList<ParticipantSessionDto> Sessions
);

public sealed record ParticipantSessionDto(
    Guid SessionId,
    DateTime ScheduledAt,
    int DurationMinutes,
    string? Location,
    bool IsPast,
    DateTime? ExcusalDeadlineAt,
    bool CanExcuse,
    string? ExcusalStatus  // null | "Excused" | "CreditIssued"
);
