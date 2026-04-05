using MediatR;
using Microsoft.EntityFrameworkCore;
using Terminar.Modules.Registrations.Domain;
using Terminar.Modules.Registrations.Infrastructure;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Registrations.Application.Queries.GetCourseEnrollmentCounts;

public sealed class GetCourseEnrollmentCountsHandler(RegistrationsDbContext db)
    : IRequestHandler<GetCourseEnrollmentCountsQuery, Dictionary<Guid, (int Enrolled, int Waitlisted)>>
{
    public async Task<Dictionary<Guid, (int Enrolled, int Waitlisted)>> Handle(
        GetCourseEnrollmentCountsQuery request,
        CancellationToken cancellationToken)
    {
        var tid = TenantId.From(request.TenantId);
        var courseIds = request.CourseIds.ToList();

        var counts = await db.Registrations
            .Where(r => r.TenantId == tid
                        && courseIds.Contains(r.CourseId)
                        && r.Status == RegistrationStatus.Confirmed)
            .GroupBy(r => r.CourseId)
            .Select(g => new { CourseId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return counts.ToDictionary(
            x => x.CourseId,
            x => (x.Count, 0));
    }
}
