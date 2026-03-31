namespace Terminar.Api.Notifications;

public sealed class StubEmailNotificationService(ILogger<StubEmailNotificationService> logger) : IEmailNotificationService
{
    public Task SendRegistrationConfirmationAsync(
        string participantEmail,
        string participantName,
        string courseTitle,
        Guid cancellationToken,
        CancellationToken ct = default)
    {
        logger.LogInformation(
            "[EMAIL STUB] Registration confirmation → {Email} ({Name}) for course '{Course}'. Cancellation token: {Token}",
            participantEmail, participantName, courseTitle, cancellationToken);
        return Task.CompletedTask;
    }

    public Task SendRegistrationCancellationAsync(
        string participantEmail,
        string participantName,
        string courseTitle,
        CancellationToken ct = default)
    {
        logger.LogInformation(
            "[EMAIL STUB] Registration cancellation → {Email} ({Name}) for course '{Course}'.",
            participantEmail, participantName, courseTitle);
        return Task.CompletedTask;
    }
}
