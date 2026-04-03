using Microsoft.EntityFrameworkCore;
using Terminar.Modules.Courses.Domain;
using Terminar.Modules.Courses.Domain.Repositories;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Courses.Infrastructure.Repositories;

public sealed class CourseRepository(CoursesDbContext db) : ICourseRepository
{
    public Task<Course?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Courses
            .Include(c => c.Sessions)
            .Include(c => c.CustomFieldAssignments)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

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
        // Course was loaded by this context so it is already tracked.
        // Calling db.Update() would mark every child entity as Modified, which
        // causes a concurrency exception for newly-created child entities whose
        // IDs don't exist in the DB yet. Scalar property changes are picked up
        // automatically by snapshot-based DetectChanges on SaveChangesAsync.

        // Reconcile CourseFieldAssignments: delete all previously-tracked rows for
        // this course, then insert the new set as Added.
        //
        // We cannot use db.Entry() to check state here — it triggers AutoDetectChanges,
        // which scans the Course navigation and marks new entities as Modified
        // (EF assumes a non-default Guid PK means the row already exists).
        // DbSet.Add() explicitly forces EntityState.Added, overriding that.
        var toDelete = db.CourseFieldAssignments.Local
            .Where(a => a.CourseId == course.Id)
            .ToList();
        db.CourseFieldAssignments.RemoveRange(toDelete);

        foreach (var assignment in course.CustomFieldAssignments)
            db.CourseFieldAssignments.Add(assignment);

        await db.SaveChangesAsync(ct);
    }
}
