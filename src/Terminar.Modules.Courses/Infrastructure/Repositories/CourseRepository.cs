using Microsoft.EntityFrameworkCore;
using Terminar.Modules.Courses.Domain;
using Terminar.Modules.Courses.Domain.Repositories;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Courses.Infrastructure.Repositories;

public sealed class CourseRepository(CoursesDbContext db) : ICourseRepository
{
    public Task<Course?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Courses.Include(c => c.Sessions).FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<List<Course>> ListByTenantAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tid = TenantId.From(tenantId);
        return db.Courses.Include(c => c.Sessions)
            .Where(c => c.TenantId == tid)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Course course, CancellationToken ct = default)
    {
        await db.Courses.AddAsync(course, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Course course, CancellationToken ct = default)
    {
        db.Courses.Update(course);
        await db.SaveChangesAsync(ct);
    }
}
