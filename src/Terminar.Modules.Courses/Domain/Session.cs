using Terminar.SharedKernel;

namespace Terminar.Modules.Courses.Domain;

public sealed class Session : Entity<Guid>
{
    public DateTime ScheduledAt { get; private set; }
    public int DurationMinutes { get; private set; }
    public string? Location { get; private set; }
    public int Sequence { get; private set; }

    private Session() { }

    public static Session Create(DateTime scheduledAt, int durationMinutes, string? location, int sequence)
    {
        if (durationMinutes < 1)
            throw new ArgumentException("Duration must be at least 1 minute.", nameof(durationMinutes));

        var utc = DateTime.SpecifyKind(scheduledAt.ToUniversalTime(), DateTimeKind.Utc);
        if (utc <= DateTime.UtcNow)
            throw new ArgumentException("Session must be scheduled in the future.", nameof(scheduledAt));

        return new Session
        {
            Id = Guid.NewGuid(),
            ScheduledAt = utc,
            DurationMinutes = durationMinutes,
            Location = location?.Trim(),
            Sequence = sequence
        };
    }

    public DateTime EndsAt => ScheduledAt.AddMinutes(DurationMinutes);
}
