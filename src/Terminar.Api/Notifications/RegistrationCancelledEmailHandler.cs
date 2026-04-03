using MediatR;
using Microsoft.EntityFrameworkCore;
using Terminar.Modules.Courses.Infrastructure;
using Terminar.Modules.Registrations.Domain.Events;

namespace Terminar.Api.Notifications;

public sealed class RegistrationCancelledEmailHandler(
    IEmailNotificationService emailService,
    CoursesDbContext coursesDb,
    ILogger<RegistrationCancelledEmailHandler> logger)
    : INotificationHandler<RegistrationCancelled>
{
    public async Task Handle(RegistrationCancelled notification, CancellationToken cancellationToken)
    {
        try
        {
            var course = await coursesDb.Courses
                .FirstOrDefaultAsync(c => c.Id == notification.CourseId, cancellationToken);
            if (course is null) return;

            if (!string.IsNullOrEmpty(notification.ParticipantEmail))
            {
                await emailService.SendUnenrollmentConfirmationAsync(
                    notification.ParticipantEmail,
                    notification.ParticipantName,
                    course.Title,
                    cancellationToken);
            }

            // TODO: Staff notification - needs course organizer email lookup (deferred to Phase G)
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send cancellation email for registration {RegistrationId}", notification.RegistrationId);
        }
    }
}
