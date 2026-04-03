using MediatR;
using Microsoft.EntityFrameworkCore;
using Terminar.Modules.Courses.Infrastructure;
using Terminar.Modules.Tenants.Domain.Events;

namespace Terminar.Modules.Courses.Application.CustomFields;

public sealed class OnCustomFieldDefinitionDeleted(CoursesDbContext db)
    : INotificationHandler<CustomFieldDefinitionDeleted>
{
    public async Task Handle(CustomFieldDefinitionDeleted notification, CancellationToken cancellationToken)
    {
        var orphaned = await db.CourseFieldAssignments
            .Where(a => a.FieldDefinitionId == notification.FieldDefinitionId)
            .ToListAsync(cancellationToken);

        if (orphaned.Count > 0)
        {
            db.CourseFieldAssignments.RemoveRange(orphaned);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
