namespace Terminar.Api.Notifications;

public static class EmailTemplates
{
    public static (string Subject, string HtmlBody, string TextBody) EnrollmentConfirmation(
        string participantName,
        string courseTitle,
        IEnumerable<(DateTime ScheduledAt, int DurationMinutes, string? Location)> sessions,
        string safeLinkUrl)
    {
        var subject = $"Enrollment confirmed: {courseTitle}";
        var sessionList = sessions.ToList();
        var sessionRows = string.Join("\n", sessionList.Select(s =>
            $"  • {s.ScheduledAt:ddd, MMM d yyyy HH:mm} ({s.DurationMinutes} min){(s.Location != null ? $" – {s.Location}" : "")}"));
        var html = $"""
            <h2>You're enrolled in {courseTitle}</h2>
            <p>Hello {participantName},</p>
            <p>Your enrollment has been confirmed. Here are your upcoming sessions:</p>
            <ul>
                {string.Join("", sessionList.Select(s => $"<li>{s.ScheduledAt:ddd, MMM d yyyy HH:mm} ({s.DurationMinutes} min){(s.Location != null ? $" – {s.Location}" : "")}</li>"))}
            </ul>
            <p><a href="{safeLinkUrl}">View your enrollment &amp; manage sessions</a></p>
            """;
        var text = $"You're enrolled in {courseTitle}\n\nHello {participantName},\n\nSessions:\n{sessionRows}\n\nManage: {safeLinkUrl}";
        return (subject, html, text);
    }

    public static (string Subject, string HtmlBody, string TextBody) MagicLink(
        string participantName,
        string magicLinkUrl)
    {
        var subject = "Your Termínář access link";
        var html = $"""
            <h2>Access your courses</h2>
            <p>Hello {participantName},</p>
            <p>Click the link below to view all your courses and excusal credits. The link expires in 15 minutes.</p>
            <p><a href="{magicLinkUrl}">Access my courses</a></p>
            """;
        var text = $"Hello {participantName},\n\nHere is your access link (valid 15 min):\n{magicLinkUrl}";
        return (subject, html, text);
    }

    public static (string Subject, string HtmlBody, string TextBody) ExcusalConfirmation(
        string participantName,
        string courseTitle,
        DateTime sessionAt,
        bool creditGenerated)
    {
        var subject = $"Excusal confirmed: {courseTitle}";
        var creditNote = creditGenerated ? "<p>An excusal credit has been added to your account.</p>" : "";
        var html = $"""
            <h2>Your excusal has been recorded</h2>
            <p>Hello {participantName},</p>
            <p>You have been excused from the session on {sessionAt:ddd, MMM d yyyy HH:mm} for <strong>{courseTitle}</strong>.</p>
            {creditNote}
            """;
        var text = $"Hello {participantName},\n\nYou've been excused from the {courseTitle} session on {sessionAt:ddd, MMM d yyyy HH:mm}.{(creditGenerated ? "\nAn excusal credit has been added to your account." : "")}";
        return (subject, html, text);
    }

    public static (string Subject, string HtmlBody, string TextBody) UnenrollmentConfirmation(
        string participantName,
        string courseTitle)
    {
        var subject = $"Unenrolled from {courseTitle}";
        var html = $"""
            <h2>Unenrollment confirmed</h2>
            <p>Hello {participantName},</p>
            <p>You have successfully unenrolled from <strong>{courseTitle}</strong>.</p>
            """;
        var text = $"Hello {participantName},\n\nYou have successfully unenrolled from {courseTitle}.";
        return (subject, html, text);
    }

    public static (string Subject, string HtmlBody, string TextBody) StaffUnenrollmentNotification(
        string participantName,
        string courseTitle)
    {
        var subject = $"[Termínář] Participant unenrolled: {courseTitle}";
        var html = $"""
            <h2>Participant unenrolled</h2>
            <p>{participantName} has unenrolled from <strong>{courseTitle}</strong>.</p>
            """;
        var text = $"{participantName} has unenrolled from {courseTitle}.";
        return (subject, html, text);
    }

    public static (string Subject, string HtmlBody, string TextBody) CreditRedemptionConfirmation(
        string participantName,
        string targetCourseTitle)
    {
        var subject = $"Credit redeemed: enrolled in {targetCourseTitle}";
        var html = $"""
            <h2>Excusal credit redeemed</h2>
            <p>Hello {participantName},</p>
            <p>Your excusal credit has been redeemed and you are now enrolled in <strong>{targetCourseTitle}</strong>.</p>
            """;
        var text = $"Hello {participantName},\n\nYour excusal credit has been redeemed. You are now enrolled in {targetCourseTitle}.";
        return (subject, html, text);
    }
}
