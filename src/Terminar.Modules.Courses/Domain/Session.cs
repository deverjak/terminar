using Terminar.SharedKernel;

namespace Terminar.Modules.Courses.Domain;

public sealed class Session : Entity<Guid>
{
    public DateTimeOffset ScheduledAt { get; private set; }
    public int DurationMinutes { get; private set; }
    public string? Location { get; private set; }
    public int Sequence { get; private set; }

    private Session() { }

    public static Session Create(DateTimeOffset scheduledAt, int durationMinutes, string? location, int sequence)
    {
        if (durationMinutes < 1)
            throw new ArgumentException("Duration must be at least 1 minute.", nameof(durationMinutes));

        return new Session
        {
            Id = Guid.NewGuid(),
            ScheduledAt = scheduledAt,
            DurationMinutes = durationMinutes,
            Location = location?.Trim(),
            Sequence = sequence
        };
    }

    public DateTimeOffset EndsAt => ScheduledAt.AddMinutes(DurationMinutes);
}
