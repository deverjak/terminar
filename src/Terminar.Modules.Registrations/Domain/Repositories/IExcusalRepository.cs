namespace Terminar.Modules.Registrations.Domain.Repositories;

public interface IExcusalRepository
{
    Task<Excusal?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<bool> ExistsForSessionAsync(Guid registrationId, Guid sessionId, CancellationToken ct = default);
    Task<List<Excusal>> ListByRegistrationAsync(Guid registrationId, CancellationToken ct = default);
    Task<List<Excusal>> ListByCourseAsync(Guid courseId, Guid tenantId, CancellationToken ct = default);
    Task AddAsync(Excusal excusal, CancellationToken ct = default);
    Task UpdateAsync(Excusal excusal, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
