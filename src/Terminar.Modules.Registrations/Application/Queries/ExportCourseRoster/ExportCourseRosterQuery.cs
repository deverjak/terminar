using MediatR;
using Terminar.Modules.Registrations.Application.Queries.GetCourseRoster;

namespace Terminar.Modules.Registrations.Application.Queries.ExportCourseRoster;

public sealed record ExportCourseRosterQuery(
    Guid TenantId,
    IReadOnlyList<Guid> CourseIds,
    bool IncludeExcusalCounts) : IRequest<ExportCourseRosterResult>;

public sealed record ExportCourseRosterResult(
    List<ExportParticipantRowDto> Participants,
    IReadOnlyList<EnabledCustomFieldDto> EnabledCustomFields);
