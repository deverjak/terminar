using Microsoft.EntityFrameworkCore;
using Terminar.Modules.Courses.Application.Ports;
using Terminar.Modules.Registrations.Domain;
using Terminar.Modules.Registrations.Domain.Repositories;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Registrations.Infrastructure.Repositories;

public sealed class RegistrationRepository(RegistrationsDbContext db) : IRegistrationRepository, IRegistrationCountReader
{
    public async Task<Registration?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        var tid = TenantId.From(tenantId);
        return await db.Registrations
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tid, ct);
    }

    public async Task<Registration?> GetByEmailAndCourseAsync(string email, Guid courseId, Guid tenantId, CancellationToken ct = default)
    {
        var tid = TenantId.From(tenantId);
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return await db.Registrations
            .FirstOrDefaultAsync(r =>
                r.CourseId == courseId &&
                r.TenantId == tid &&
                r.ParticipantEmail.Value == normalizedEmail &&
                r.Status == RegistrationStatus.Confirmed, ct);
    }

    public async Task<int> CountConfirmedByCourseAsync(Guid courseId, Guid tenantId, CancellationToken ct = default)
    {
        var tid = TenantId.From(tenantId);
        return await db.Registrations
            .CountAsync(r => r.CourseId == courseId && r.TenantId == tid && r.Status == RegistrationStatus.Confirmed, ct);
    }

    public async Task<int> CountConfirmedAsync(Guid courseId, Guid tenantId, CancellationToken ct = default) =>
        await CountConfirmedByCourseAsync(courseId, tenantId, ct);

    public async Task AddAsync(Registration registration, CancellationToken ct = default)
    {
        db.Registrations.Add(registration);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Registration registration, CancellationToken ct = default)
    {
        db.Registrations.Update(registration);
        await db.SaveChangesAsync(ct);
    }
}
