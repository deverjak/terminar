namespace Terminar.Modules.Registrations.Domain.Repositories;

public interface IExcusalCreditRepository
{
    Task<ExcusalCredit?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<List<ExcusalCredit>> ListByParticipantEmailAsync(string email, Guid tenantId, CancellationToken ct = default);
    Task<(List<ExcusalCredit> Items, int Total)> ListByTenantAsync(Guid tenantId, ExcusalCreditStatus? status, string? participantEmail, int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(ExcusalCredit credit, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
