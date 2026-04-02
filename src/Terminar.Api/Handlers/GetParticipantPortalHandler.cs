using MediatR;
using Microsoft.EntityFrameworkCore;
using Terminar.Modules.Courses.Infrastructure;
using Terminar.Modules.Registrations.Application.Queries.GetParticipantPortal;
using Terminar.Modules.Registrations.Domain;
using Terminar.Modules.Registrations.Domain.Repositories;
using Terminar.Modules.Tenants.Infrastructure;

namespace Terminar.Api.Handlers;

public sealed class GetParticipantPortalHandler(
    IParticipantMagicLinkRepository magicLinkRepo,
    IRegistrationRepository registrationRepo,
    IExcusalCreditRepository creditRepo,
    CoursesDbContext coursesDb,
    TenantsDbContext tenantsDb)
    : IRequestHandler<GetParticipantPortalQuery, ParticipantPortalDto?>
{
    public async Task<ParticipantPortalDto?> Handle(GetParticipantPortalQuery request, CancellationToken cancellationToken)
    {
        var session = await magicLinkRepo.GetByPortalTokenAsync(request.PortalToken, cancellationToken);
        if (session is null || !session.IsPortalTokenValid || session.TenantId.Value != request.TenantId)
            return null;

        var email = session.ParticipantEmail.Value;

        var registrations = await registrationRepo.ListByEmailAndTenantAsync(email, request.TenantId, cancellationToken);

        var tenantRecord = await tenantsDb.Tenants.FirstOrDefaultAsync(t => t.Id.Value == request.TenantId, cancellationToken);
        var deadlineDays = tenantRecord?.ExcusalSettings.UnenrollmentDeadlineDays ?? 14;

        var enrollmentDtos = new List<PortalEnrollmentDto>();
        foreach (var reg in registrations.Where(r => r.Status == RegistrationStatus.Confirmed))
        {
            var course = await coursesDb.Courses
                .Include(c => c.Sessions)
                .FirstOrDefaultAsync(c => c.Id == reg.CourseId, cancellationToken);
            if (course is null) continue;

            var firstSession = course.Sessions.OrderBy(s => s.ScheduledAt).FirstOrDefault();
            DateTime? unenrollDeadline = firstSession is not null
                ? firstSession.ScheduledAt.AddDays(-deadlineDays)
                : null;
            bool canUnenroll = unenrollDeadline is null || DateTime.UtcNow < unenrollDeadline;

            enrollmentDtos.Add(new PortalEnrollmentDto(
                reg.Id, reg.SafeLinkToken, course.Id, course.Title,
                reg.Status.ToString(), firstSession?.ScheduledAt, unenrollDeadline, canUnenroll));
        }

        var credits = await creditRepo.ListByParticipantEmailAsync(email, request.TenantId, cancellationToken);

        var now = DateTime.UtcNow;
        var creditDtos = new List<PortalCreditDto>();
        foreach (var credit in credits)
        {
            var status = credit.Status;
            if (status == ExcusalCreditStatus.Active)
            {
                var today = DateOnly.FromDateTime(now);
                var validWindows = await tenantsDb.ExcusalValidityWindows
                    .Where(w => w.TenantId.Value == request.TenantId && credit.ValidWindowIds.Contains(w.Id))
                    .ToListAsync(cancellationToken);
                var lastWindowEnd = validWindows.Any() ? validWindows.Max(w => w.EndDate) : (DateOnly?)null;
                if (lastWindowEnd.HasValue && today > lastWindowEnd.Value)
                    status = ExcusalCreditStatus.Expired;
            }

            var sourceCourse = await coursesDb.Courses
                .FirstOrDefaultAsync(c => c.Id == credit.SourceCourseId, cancellationToken);

            creditDtos.Add(new PortalCreditDto(
                credit.Id,
                sourceCourse?.Title ?? "Unknown course",
                null,
                credit.Tags,
                null,
                status.ToString()));
        }

        var participantName = registrations.FirstOrDefault()?.ParticipantName ?? email;
        return new ParticipantPortalDto(email, participantName, enrollmentDtos, creditDtos);
    }
}
