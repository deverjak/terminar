using MediatR;
using Microsoft.EntityFrameworkCore;
using Terminar.Modules.Courses.Infrastructure;
using Terminar.Modules.Registrations.Domain.Events;

namespace Terminar.Api.Notifications;

public sealed class ExcusalCreatedEmailHandler(
    IEmailNotificationService emailService,
    CoursesDbContext coursesDb,
    ILogger<ExcusalCreatedEmailHandler> logger)
    : INotificationHandler<ExcusalCreated>
{
    public async Task Handle(ExcusalCreated notification, CancellationToken cancellationToken)
    {
        try
        {
            var course = await coursesDb.Courses
                .Include(c => c.Sessions)
                .FirstOrDefaultAsync(c => c.Id == notification.CourseId, cancellationToken);

            if (course is null) return;

            var session = course.Sessions.FirstOrDefault(s => s.Id == notification.SessionId);
            if (session is null) return;

            await emailService.SendExcusalConfirmationAsync(
                notification.ParticipantEmail,
                notification.ParticipantName,
                course.Title,
                session.ScheduledAt,
                false,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send excusal email for excusal {ExcusalId}", notification.ExcusalId);
        }
    }
}
