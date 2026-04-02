using MediatR;
using Microsoft.EntityFrameworkCore;
using Terminar.Modules.Courses.Infrastructure;
using Terminar.Modules.Registrations.Domain.Events;

namespace Terminar.Api.Notifications;

public sealed class ExcusalCreditRedeemedEmailHandler(
    IEmailNotificationService emailService,
    CoursesDbContext coursesDb,
    ILogger<ExcusalCreditRedeemedEmailHandler> logger)
    : INotificationHandler<ExcusalCreditRedeemed>
{
    public async Task Handle(ExcusalCreditRedeemed notification, CancellationToken cancellationToken)
    {
        try
        {
            var course = await coursesDb.Courses
                .FirstOrDefaultAsync(c => c.Id == notification.RedeemedCourseId, cancellationToken);
            if (course is null) return;

            await emailService.SendCreditRedemptionConfirmationAsync(
                notification.ParticipantEmail,
                notification.ParticipantEmail,
                course.Title,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send credit redemption email for credit {CreditId}", notification.CreditId);
        }
    }
}
