using MediatR;
using Terminar.Modules.Registrations.Domain.Events;

namespace Terminar.Api.Notifications;

public sealed class RegistrationCreatedEmailHandler(IEmailNotificationService emailService, ILogger<RegistrationCreatedEmailHandler> logger)
    : INotificationHandler<RegistrationCreated>
{
    public async Task Handle(RegistrationCreated notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Sending registration confirmation email for registration {RegistrationId}",
            notification.RegistrationId);

        // Title lookup skipped — stub uses participant email as placeholder
        await emailService.SendRegistrationConfirmationAsync(
            notification.ParticipantEmail,
            notification.ParticipantEmail,
            $"Course {notification.CourseId}",
            notification.RegistrationId,
            cancellationToken);
    }
}
