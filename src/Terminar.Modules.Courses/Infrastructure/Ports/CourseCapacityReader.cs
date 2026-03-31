using Microsoft.EntityFrameworkCore;
using Terminar.Modules.Courses.Application.Ports;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Courses.Infrastructure.Ports;

public sealed class CourseCapacityReader(CoursesDbContext db, IRegistrationCountReader registrationCountReader) : ICourseCapacityReader
{
    public async Task<CourseCapacityInfo?> GetCapacityInfoAsync(Guid courseId, Guid tenantId, CancellationToken ct = default)
    {
        var tid = TenantId.From(tenantId);
        var course = await db.Courses
            .Include(c => c.Sessions)
            .FirstOrDefaultAsync(c => c.Id == courseId && c.TenantId == tid, ct);

        if (course is null) return null;

        var confirmedCount = await registrationCountReader.CountConfirmedAsync(courseId, tenantId, ct);

        var lastSessionEndsAt = course.Sessions.Count > 0
            ? course.Sessions.Max(s => s.EndsAt)
            : (DateTime?)null;

        return new CourseCapacityInfo(
            course.Capacity,
            confirmedCount,
            course.Status.ToString(),
            course.RegistrationMode.ToString(),
            lastSessionEndsAt);
    }
}
