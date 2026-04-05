using MediatR;
using Terminar.Modules.Courses.Domain;

namespace Terminar.Modules.Courses.Application.Queries.ExportCourses;

public sealed record ExportCoursesQuery(
    Guid TenantId,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    CourseStatus? Status) : IRequest<List<ExportCourseRowDto>>;
