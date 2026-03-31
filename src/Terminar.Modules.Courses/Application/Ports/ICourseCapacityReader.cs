namespace Terminar.Modules.Courses.Application.Ports;

public sealed record CourseCapacityInfo(
    int Capacity,
    int ConfirmedCount,
    string Status,
    string RegistrationMode,
    DateTime? LastSessionEndsAt);

public interface ICourseCapacityReader
{
    Task<CourseCapacityInfo?> GetCapacityInfoAsync(Guid courseId, Guid tenantId, CancellationToken ct = default);
}
