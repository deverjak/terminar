namespace Terminar.Api.Notifications;

public interface IEmailNotificationService
{
    Task SendRegistrationConfirmationAsync(
        string participantEmail,
        string participantName,
        string courseTitle,
        Guid cancellationToken,
        CancellationToken ct = default);

    Task SendRegistrationCancellationAsync(
        string participantEmail,
        string participantName,
        string courseTitle,
        CancellationToken ct = default);
}
