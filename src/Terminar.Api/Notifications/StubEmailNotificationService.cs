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

    public Task SendEnrollmentConfirmationAsync(
        string participantEmail,
        string participantName,
        string courseTitle,
        IEnumerable<(DateTime ScheduledAt, int DurationMinutes, string? Location)> sessions,
        string safeLinkUrl,
        CancellationToken ct = default)
    {
        logger.LogInformation(
            "[EMAIL STUB] Enrollment confirmation → {Email} ({Name}) for course '{Course}'. SafeLink: {Url}",
            participantEmail, participantName, courseTitle, safeLinkUrl);
        return Task.CompletedTask;
    }

    public Task SendMagicLinkAsync(
        string participantEmail,
        string participantName,
        string magicLinkUrl,
        CancellationToken ct = default)
    {
        logger.LogInformation(
            "[EMAIL STUB] Magic link → {Email} ({Name}). Url: {Url}",
            participantEmail, participantName, magicLinkUrl);
        return Task.CompletedTask;
    }

    public Task SendExcusalConfirmationAsync(
        string participantEmail,
        string participantName,
        string courseTitle,
        DateTime sessionAt,
        bool creditGenerated,
        CancellationToken ct = default)
    {
        logger.LogInformation(
            "[EMAIL STUB] Excusal confirmation → {Email} ({Name}) for '{Course}' session at {At}. Credit: {Credit}",
            participantEmail, participantName, courseTitle, sessionAt, creditGenerated);
        return Task.CompletedTask;
    }

    public Task SendUnenrollmentConfirmationAsync(
        string participantEmail,
        string participantName,
        string courseTitle,
        CancellationToken ct = default)
    {
        logger.LogInformation(
            "[EMAIL STUB] Unenrollment confirmation → {Email} ({Name}) from '{Course}'.",
            participantEmail, participantName, courseTitle);
        return Task.CompletedTask;
    }

    public Task SendStaffUnenrollmentNotificationAsync(
        string staffEmail,
        string participantName,
        string courseTitle,
        CancellationToken ct = default)
    {
        logger.LogInformation(
            "[EMAIL STUB] Staff unenrollment notification → {Email}: {Name} unenrolled from '{Course}'.",
            staffEmail, participantName, courseTitle);
        return Task.CompletedTask;
    }

    public Task SendCreditRedemptionConfirmationAsync(
        string participantEmail,
        string participantName,
        string targetCourseTitle,
        CancellationToken ct = default)
    {
        logger.LogInformation(
            "[EMAIL STUB] Credit redemption confirmation → {Email} ({Name}) for '{Course}'.",
            participantEmail, participantName, targetCourseTitle);
        return Task.CompletedTask;
    }
}
