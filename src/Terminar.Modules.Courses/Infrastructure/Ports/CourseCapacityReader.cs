using Microsoft.EntityFrameworkCore;
using Terminar.Modules.Courses.Application.Ports;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Courses.Infrastructure.Ports;

public sealed class CourseCapacityReader(CoursesDbContext db) : ICourseCapacityReader
{
    public async Task<CourseCapacityInfo?> GetCapacityInfoAsync(Guid courseId, Guid tenantId, CancellationToken ct = default)
    {
        var tid = TenantId.From(tenantId);
        var course = await db.Courses
            .Where(c => c.Id == courseId && c.TenantId == tid)
            .Select(c => new { c.Capacity })
            .FirstOrDefaultAsync(ct);

        if (course is null) return null;

        // Confirmed count will be provided by the Registrations module via its own port implementation.
        // This reader only returns the capacity; the confirmed count is 0 until Registrations is wired up.
        return new CourseCapacityInfo(course.Capacity, 0);
    }
}
