using MediatR;

namespace Terminar.Modules.Registrations.Application.Queries.GetCourseRoster;

public sealed record GetCourseRosterQuery(
    Guid CourseId,
    Guid TenantId,
    string? StatusFilter,
    int Page,
    int PageSize) : IRequest<GetCourseRosterResult>;

public sealed record RegistrationDto(
    Guid RegistrationId,
    string ParticipantName,
    string ParticipantEmail,
    string RegistrationSource,
    string Status,
    DateTime RegisteredAt);

public sealed record GetCourseRosterResult(
    IReadOnlyList<RegistrationDto> Items,
    int Total,
    int Page,
    int PageSize);
