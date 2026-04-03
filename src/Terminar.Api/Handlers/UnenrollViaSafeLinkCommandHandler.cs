using MediatR;
using Microsoft.EntityFrameworkCore;
using Terminar.Modules.Courses.Infrastructure;
using Terminar.Modules.Registrations.Application.Commands.UnenrollViaSafeLink;
using Terminar.Modules.Registrations.Domain.Repositories;
using Terminar.Modules.Tenants.Infrastructure;
using Terminar.SharedKernel;

namespace Terminar.Api.Handlers;

public sealed class UnenrollViaSafeLinkCommandHandler(
    IRegistrationRepository registrationRepo,
    CoursesDbContext coursesDb,
    TenantsDbContext tenantsDb)
    : IRequestHandler<UnenrollViaSafeLinkCommand>
{
    public async Task Handle(UnenrollViaSafeLinkCommand request, CancellationToken cancellationToken)
    {
        var registration = await registrationRepo.GetBySafeLinkTokenAsync(
            request.SafeLinkToken, request.TenantId, cancellationToken);

        if (registration is null)
            throw new NotFoundException("Enrollment not found.");

        if (registration.Status == Terminar.Modules.Registrations.Domain.RegistrationStatus.Cancelled)
            throw new UnprocessableException("Already unenrolled.");

        // Check unenrollment deadline
        var tenantSettings = await tenantsDb.Tenants
            .FirstOrDefaultAsync(t => t.Id.Value == request.TenantId, cancellationToken);
        var deadlineDays = tenantSettings?.ExcusalSettings.UnenrollmentDeadlineDays ?? 14;

        var course = await coursesDb.Courses
            .Include(c => c.Sessions)
            .FirstOrDefaultAsync(c => c.Id == registration.CourseId, cancellationToken);

        var firstSession = course?.Sessions.OrderBy(s => s.ScheduledAt).FirstOrDefault();
        var now = DateTime.UtcNow;

        if (firstSession is not null)
        {
            var deadline = firstSession.ScheduledAt.AddDays(-deadlineDays);
            if (now >= deadline)
                throw new UnprocessableException($"Unenrollment deadline has passed. Contact the organizer.");
        }

        var lastSession = course?.Sessions.OrderByDescending(s => s.ScheduledAt).FirstOrDefault();
        var lastSessionEndsAt = lastSession is not null
            ? lastSession.ScheduledAt.AddMinutes(lastSession.DurationMinutes)
            : (DateTime?)null;

        registration.Cancel(now, lastSessionEndsAt, "Self-unenrolled via safe link");
        await registrationRepo.UpdateAsync(registration, cancellationToken);
    }
}
