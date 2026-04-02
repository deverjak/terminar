using MediatR;
using Microsoft.EntityFrameworkCore;
using Terminar.Modules.Courses.Infrastructure;
using Terminar.Modules.Registrations.Domain.Events;

namespace Terminar.Api.Notifications;

public sealed class RegistrationCreatedEmailHandler(
    IEmailNotificationService emailService,
    CoursesDbContext coursesDb,
    IConfiguration configuration,
    ILogger<RegistrationCreatedEmailHandler> logger)
    : INotificationHandler<RegistrationCreated>
{
    public async Task Handle(RegistrationCreated notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Sending enrollment confirmation email for registration {RegistrationId}", notification.RegistrationId);

        try
        {
            var course = await coursesDb.Courses
                .Include(c => c.Sessions)
                .FirstOrDefaultAsync(c => c.Id == notification.CourseId, cancellationToken);

            if (course is null)
            {
                logger.LogWarning("Course {CourseId} not found — skipping enrollment email", notification.CourseId);
                return;
            }

            var baseUrl = configuration["App:BaseUrl"] ?? "http://localhost:5173";
            var safeLinkUrl = $"{baseUrl}/participant/course/{notification.SafeLinkToken}";

            var sessions = course.Sessions
                .OrderBy(s => s.ScheduledAt)
                .Select(s => (s.ScheduledAt, s.DurationMinutes, s.Location));

            await emailService.SendEnrollmentConfirmationAsync(
                notification.ParticipantEmail,
                notification.ParticipantName,
                course.Title,
                sessions,
                safeLinkUrl,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send enrollment email for registration {RegistrationId}", notification.RegistrationId);
        }
    }
}
