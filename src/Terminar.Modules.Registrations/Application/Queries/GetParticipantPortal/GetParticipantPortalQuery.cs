using MediatR;

namespace Terminar.Modules.Registrations.Application.Queries.GetParticipantPortal;

public sealed record GetParticipantPortalQuery(string PortalToken, Guid TenantId)
    : IRequest<ParticipantPortalDto?>;

public sealed record ParticipantPortalDto(
    string ParticipantEmail,
    string ParticipantName,
    IReadOnlyList<PortalEnrollmentDto> Enrollments,
    IReadOnlyList<PortalCreditDto> ExcusalCredits
);

public sealed record PortalEnrollmentDto(
    Guid EnrollmentId,
    Guid SafeLinkToken,
    Guid CourseId,
    string CourseTitle,
    string Status,
    DateTime? FirstSessionAt,
    DateTime? UnenrollmentDeadlineAt,
    bool CanUnenroll
);

public sealed record PortalCreditDto(
    Guid CreditId,
    string SourceCourseTitle,
    DateTime? SourceSessionAt,
    IReadOnlyList<string> Tags,
    string? ValidUntil,
    string Status
);
