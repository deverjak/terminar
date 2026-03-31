using Terminar.Modules.Courses.Application.Ports;
using Terminar.SharedKernel;

namespace Terminar.Modules.Registrations.Domain.Services;

public sealed class RegistrationCapacityChecker(ICourseCapacityReader capacityReader)
{
    public async Task<CourseCapacityInfo> EnsureCanRegisterAsync(Guid courseId, Guid tenantId, CancellationToken ct = default)
    {
        var info = await capacityReader.GetCapacityInfoAsync(courseId, tenantId, ct);

        if (info is null)
            throw new NotFoundException($"Course '{courseId}' not found.");

        if (info.Status is "Cancelled" or "Completed")
            throw new UnprocessableException($"Cannot register for a course with status '{info.Status}'.");

        if (info.ConfirmedCount >= info.Capacity)
            throw new UnprocessableException($"Course '{courseId}' is at full capacity ({info.Capacity}/{info.Capacity}).");

        return info;
    }
}
