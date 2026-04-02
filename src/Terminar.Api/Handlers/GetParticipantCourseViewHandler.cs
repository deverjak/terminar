using MediatR;
using Microsoft.EntityFrameworkCore;
using Terminar.Modules.Courses.Infrastructure;
using Terminar.Modules.Registrations.Application.Queries.GetParticipantCourseView;
using Terminar.Modules.Registrations.Domain;
using Terminar.Modules.Registrations.Domain.Repositories;
using Terminar.Modules.Tenants.Infrastructure;

namespace Terminar.Api.Handlers;

public sealed class GetParticipantCourseViewHandler(
    IRegistrationRepository registrationRepo,
    CoursesDbContext coursesDb,
    TenantsDbContext tenantsDb)
    : IRequestHandler<GetParticipantCourseViewQuery, ParticipantCourseViewDto?>
{
    public async Task<ParticipantCourseViewDto?> Handle(GetParticipantCourseViewQuery request, CancellationToken cancellationToken)
    {
        var registration = await registrationRepo.GetBySafeLinkTokenAsync(request.SafeLinkToken, request.TenantId, cancellationToken);
        if (registration is null || registration.Status == RegistrationStatus.Cancelled)
            return null;

        var course = await coursesDb.Courses
            .Include(c => c.Sessions)
            .FirstOrDefaultAsync(c => c.Id == registration.CourseId, cancellationToken);
        if (course is null) return null;

        var tenantSettings = await tenantsDb.Tenants
            .FirstOrDefaultAsync(t => t.Id.Value == request.TenantId, cancellationToken);
        var settings = tenantSettings?.ExcusalSettings;
        var deadlineDays = settings?.UnenrollmentDeadlineDays ?? 14;
        var deadlineHours = settings?.ExcusalDeadlineHours ?? 24;

        var now = DateTime.UtcNow;
        var firstSession = course.Sessions.OrderBy(s => s.ScheduledAt).FirstOrDefault();
        DateTime? unenrollDeadline = firstSession is not null
            ? firstSession.ScheduledAt.AddDays(-deadlineDays)
            : null;
        bool canUnenroll = registration.Status == RegistrationStatus.Confirmed
            && (unenrollDeadline is null || now < unenrollDeadline);

        var sessionDtos = course.Sessions
            .OrderBy(s => s.ScheduledAt)
            .Select(s =>
            {
                var isPast = now > s.ScheduledAt.AddMinutes(s.DurationMinutes);
                var excusalDeadline = s.ScheduledAt.AddHours(-deadlineHours);
                var canExcuse = !isPast && now < excusalDeadline;
                return new ParticipantSessionDto(
                    s.Id,
                    s.ScheduledAt,
                    s.DurationMinutes,
                    s.Location,
                    isPast,
                    excusalDeadline,
                    canExcuse,
                    null  // excusal status - will be populated in Phase 6 when Excusal entity exists
                );
            })
            .ToList();

        return new ParticipantCourseViewDto(
            registration.Id,
            course.Id,
            course.Title,
            course.Status.ToString(),
            registration.ParticipantName,
            registration.Status.ToString(),
            unenrollDeadline,
            canUnenroll,
            sessionDtos
        );
    }
}
