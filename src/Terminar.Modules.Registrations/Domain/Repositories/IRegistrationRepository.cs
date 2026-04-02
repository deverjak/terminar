namespace Terminar.Modules.Registrations.Domain.Repositories;

public interface IRegistrationRepository
{
    Task<Registration?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<Registration?> GetByEmailAndCourseAsync(string email, Guid courseId, Guid tenantId, CancellationToken ct = default);
    Task<Registration?> GetBySafeLinkTokenAsync(Guid safeLinkToken, Guid tenantId, CancellationToken ct = default);
    Task<int> CountConfirmedByCourseAsync(Guid courseId, Guid tenantId, CancellationToken ct = default);
    Task<bool> HasActiveRegistrationsAsync(Guid tenantId, string participantEmail, CancellationToken ct = default);
    Task<List<Registration>> ListByEmailAndTenantAsync(string email, Guid tenantId, CancellationToken ct = default);
    Task AddAsync(Registration registration, CancellationToken ct = default);
    Task UpdateAsync(Registration registration, CancellationToken ct = default);
}
