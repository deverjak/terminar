using MediatR;
using Microsoft.EntityFrameworkCore;
using Terminar.Modules.Courses.Infrastructure;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Courses.Application.Queries.ExportCourses;

public sealed class ExportCoursesHandler(CoursesDbContext db)
    : IRequestHandler<ExportCoursesQuery, List<ExportCourseRowDto>>
{
    public async Task<List<ExportCourseRowDto>> Handle(
        ExportCoursesQuery request,
        CancellationToken cancellationToken)
    {
        var tid = TenantId.From(request.TenantId);

        var query = db.Courses
            .Include(c => c.Sessions)
            .Where(c => c.TenantId == tid);

        if (request.Status.HasValue)
            query = query.Where(c => c.Status == request.Status.Value);

        var courses = await query.ToListAsync(cancellationToken);

        return courses
            .Select(c =>
            {
                var firstSession = c.Sessions.OrderBy(s => s.ScheduledAt).FirstOrDefault();
                var lastSession = c.Sessions.OrderBy(s => s.ScheduledAt).LastOrDefault();

                return new ExportCourseRowDto(
                    c.Id,
                    c.Title,
                    c.Description,
                    c.CourseType.ToString(),
                    c.RegistrationMode.ToString(),
                    c.Capacity,
                    c.Status.ToString(),
                    firstSession != null ? DateOnly.FromDateTime(firstSession.ScheduledAt) : null,
                    lastSession != null ? DateOnly.FromDateTime(lastSession.EndsAt) : null,
                    firstSession?.Location);
            })
            .Where(c =>
            {
                if (request.DateFrom.HasValue && c.FirstSessionAt.HasValue &&
                    c.FirstSessionAt.Value < request.DateFrom.Value)
                    return false;
                if (request.DateTo.HasValue && c.FirstSessionAt.HasValue &&
                    c.FirstSessionAt.Value > request.DateTo.Value)
                    return false;
                return true;
            })
            .ToList();
    }
}
