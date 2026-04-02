using Microsoft.EntityFrameworkCore;
using Terminar.Modules.Registrations.Domain;
using Terminar.Modules.Registrations.Domain.Repositories;

namespace Terminar.Modules.Registrations.Infrastructure.Repositories;

public sealed class ExcusalCreditRepository(RegistrationsDbContext db) : IExcusalCreditRepository
{
    public async Task<ExcusalCredit?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
        => await db.ExcusalCredits
            .Include(x => x.AuditEntries)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId.Value == tenantId, ct);

    public async Task<List<ExcusalCredit>> ListByParticipantEmailAsync(string email, Guid tenantId, CancellationToken ct = default)
        => await db.ExcusalCredits
            .Where(x => x.ParticipantEmail == email && x.TenantId.Value == tenantId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

    public async Task<(List<ExcusalCredit> Items, int Total)> ListByTenantAsync(
        Guid tenantId, ExcusalCreditStatus? status, string? participantEmail, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.ExcusalCredits
            .Include(x => x.AuditEntries)
            .Where(x => x.TenantId.Value == tenantId);

        if (status.HasValue) query = query.Where(x => x.Status == status);
        if (!string.IsNullOrEmpty(participantEmail)) query = query.Where(x => x.ParticipantEmail == participantEmail);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task AddAsync(ExcusalCredit credit, CancellationToken ct = default)
        => await db.ExcusalCredits.AddAsync(credit, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
