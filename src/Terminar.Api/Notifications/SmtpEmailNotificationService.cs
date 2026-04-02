using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Terminar.Api.Infrastructure;

namespace Terminar.Api.Notifications;

public sealed class SmtpEmailNotificationService(
    IOptions<SmtpSettings> smtpOptions,
    ILogger<SmtpEmailNotificationService> logger)
    : IEmailNotificationService
{
    private readonly SmtpSettings _smtp = smtpOptions.Value;

    // Keep old method for compatibility
    public Task SendRegistrationConfirmationAsync(string participantEmail, string participantName, string courseTitle, Guid cancellationToken, CancellationToken ct = default)
    {
        // Stub - superseded by SendEnrollmentConfirmationAsync
        logger.LogInformation("[SMTP] Legacy registration confirmation stub for {Email}", participantEmail);
        return Task.CompletedTask;
    }

    public Task SendRegistrationCancellationAsync(string participantEmail, string participantName, string courseTitle, CancellationToken ct = default)
    {
        var (subject, html, text) = EmailTemplates.UnenrollmentConfirmation(participantName, courseTitle);
        return SendEmailAsync(participantEmail, subject, html, text, ct);
    }

    public Task SendEnrollmentConfirmationAsync(string participantEmail, string participantName, string courseTitle, IEnumerable<(DateTime ScheduledAt, int DurationMinutes, string? Location)> sessions, string safeLinkUrl, CancellationToken ct = default)
    {
        var (subject, html, text) = EmailTemplates.EnrollmentConfirmation(participantName, courseTitle, sessions, safeLinkUrl);
        return SendEmailAsync(participantEmail, subject, html, text, ct);
    }

    public Task SendMagicLinkAsync(string participantEmail, string participantName, string magicLinkUrl, CancellationToken ct = default)
    {
        var (subject, html, text) = EmailTemplates.MagicLink(participantName, magicLinkUrl);
        return SendEmailAsync(participantEmail, subject, html, text, ct);
    }

    public Task SendExcusalConfirmationAsync(string participantEmail, string participantName, string courseTitle, DateTime sessionAt, bool creditGenerated, CancellationToken ct = default)
    {
        var (subject, html, text) = EmailTemplates.ExcusalConfirmation(participantName, courseTitle, sessionAt, creditGenerated);
        return SendEmailAsync(participantEmail, subject, html, text, ct);
    }

    public Task SendUnenrollmentConfirmationAsync(string participantEmail, string participantName, string courseTitle, CancellationToken ct = default)
    {
        var (subject, html, text) = EmailTemplates.UnenrollmentConfirmation(participantName, courseTitle);
        return SendEmailAsync(participantEmail, subject, html, text, ct);
    }

    public Task SendStaffUnenrollmentNotificationAsync(string staffEmail, string participantName, string courseTitle, CancellationToken ct = default)
    {
        var (subject, html, text) = EmailTemplates.StaffUnenrollmentNotification(participantName, courseTitle);
        return SendEmailAsync(staffEmail, subject, html, text, ct);
    }

    public Task SendCreditRedemptionConfirmationAsync(string participantEmail, string participantName, string targetCourseTitle, CancellationToken ct = default)
    {
        var (subject, html, text) = EmailTemplates.CreditRedemptionConfirmation(participantName, targetCourseTitle);
        return SendEmailAsync(participantEmail, subject, html, text, ct);
    }

    private async Task SendEmailAsync(string toAddress, string subject, string htmlBody, string textBody, CancellationToken ct)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_smtp.FromName, _smtp.FromAddress));
            message.To.Add(MailboxAddress.Parse(toAddress));
            message.Subject = subject;

            var builder = new BodyBuilder
            {
                HtmlBody = htmlBody,
                TextBody = textBody
            };
            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            var secureSocketOptions = _smtp.UseSsl
                ? SecureSocketOptions.SslOnConnect
                : _smtp.UseStartTls
                    ? SecureSocketOptions.StartTlsWhenAvailable
                    : SecureSocketOptions.None;

            await client.ConnectAsync(_smtp.Host, _smtp.Port, secureSocketOptions, ct);

            if (!string.IsNullOrEmpty(_smtp.Username))
                await client.AuthenticateAsync(_smtp.Username, _smtp.Password, ct);

            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            logger.LogInformation("Email sent to {ToAddress}: {Subject}", toAddress, subject);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {ToAddress}: {Subject}", toAddress, subject);
            // Do NOT rethrow — email failures must not break the primary flow
        }
    }
}
