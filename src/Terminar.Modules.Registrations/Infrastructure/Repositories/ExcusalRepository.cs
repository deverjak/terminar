using Microsoft.EntityFrameworkCore;
using Terminar.Modules.Registrations.Domain;
using Terminar.Modules.Registrations.Domain.Repositories;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Registrations.Infrastructure.Repositories;

public sealed class ExcusalRepository(RegistrationsDbContext db) : IExcusalRepository
{
    public async Task<Excusal?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        var tid = TenantId.From(tenantId);
        return await db.Excusals.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tid, ct);
    }

    public async Task<bool> ExistsForSessionAsync(Guid registrationId, Guid sessionId, CancellationToken ct = default)
        => await db.Excusals.AnyAsync(x => x.RegistrationId == registrationId && x.SessionId == sessionId, ct);

    public async Task<List<Excusal>> ListByRegistrationAsync(Guid registrationId, CancellationToken ct = default)
        => await db.Excusals.Where(x => x.RegistrationId == registrationId).ToListAsync(ct);

    public async Task<List<Excusal>> ListByCourseAsync(Guid courseId, Guid tenantId, CancellationToken ct = default)
    {
        var tid = TenantId.From(tenantId);
        return await db.Excusals.Where(x => x.CourseId == courseId && x.TenantId == tid).ToListAsync(ct);
    }

    public async Task AddAsync(Excusal excusal, CancellationToken ct = default)
        => await db.Excusals.AddAsync(excusal, ct);

    public Task UpdateAsync(Excusal excusal, CancellationToken ct = default)
    {
        db.Excusals.Update(excusal);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
