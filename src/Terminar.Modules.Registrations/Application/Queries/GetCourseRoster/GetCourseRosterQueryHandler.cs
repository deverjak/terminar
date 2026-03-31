using MediatR;
using Microsoft.EntityFrameworkCore;
using Terminar.Modules.Registrations.Domain;
using Terminar.Modules.Registrations.Infrastructure;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Registrations.Application.Queries.GetCourseRoster;

public sealed class GetCourseRosterQueryHandler(RegistrationsDbContext db) : IRequestHandler<GetCourseRosterQuery, GetCourseRosterResult>
{
    public async Task<GetCourseRosterResult> Handle(GetCourseRosterQuery request, CancellationToken cancellationToken)
    {
        var tid = TenantId.From(request.TenantId);

        var query = db.Registrations
            .Where(r => r.CourseId == request.CourseId && r.TenantId == tid);

        if (!string.IsNullOrWhiteSpace(request.StatusFilter) &&
            Enum.TryParse<RegistrationStatus>(request.StatusFilter, ignoreCase: true, out var status))
        {
            query = query.Where(r => r.Status == status);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(r => r.RegisteredAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new RegistrationDto(
                r.Id,
                r.ParticipantName,
                r.ParticipantEmail.Value,
                r.RegistrationSource.ToString(),
                r.Status.ToString(),
                r.RegisteredAt))
            .ToListAsync(cancellationToken);

        return new GetCourseRosterResult(items, total, request.Page, request.PageSize);
    }
}
