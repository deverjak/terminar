namespace Terminar.Modules.Courses.Application.Ports;

public interface IRegistrationCountReader
{
    Task<int> CountConfirmedAsync(Guid courseId, Guid tenantId, CancellationToken ct = default);
}
