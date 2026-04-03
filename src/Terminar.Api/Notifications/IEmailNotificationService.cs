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

    Task SendEnrollmentConfirmationAsync(
        string participantEmail,
        string participantName,
        string courseTitle,
        IEnumerable<(DateTime ScheduledAt, int DurationMinutes, string? Location)> sessions,
        string safeLinkUrl,
        CancellationToken ct = default);

    Task SendMagicLinkAsync(
        string participantEmail,
        string participantName,
        string magicLinkUrl,
        CancellationToken ct = default);

    Task SendExcusalConfirmationAsync(
        string participantEmail,
        string participantName,
        string courseTitle,
        DateTime sessionAt,
        bool creditGenerated,
        CancellationToken ct = default);

    Task SendUnenrollmentConfirmationAsync(
        string participantEmail,
        string participantName,
        string courseTitle,
        CancellationToken ct = default);

    Task SendStaffUnenrollmentNotificationAsync(
        string staffEmail,
        string participantName,
        string courseTitle,
        CancellationToken ct = default);

    Task SendCreditRedemptionConfirmationAsync(
        string participantEmail,
        string participantName,
        string targetCourseTitle,
        CancellationToken ct = default);
}
