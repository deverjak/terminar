using MediatR;

namespace Terminar.Modules.Registrations.Application.Queries.GetCourseEnrollmentCounts;

public sealed record GetCourseEnrollmentCountsQuery(
    Guid TenantId,
    IReadOnlyList<Guid> CourseIds) : IRequest<Dictionary<Guid, (int Enrolled, int Waitlisted)>>;
