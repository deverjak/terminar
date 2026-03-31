using Terminar.Modules.Courses.Domain.Events;
using Terminar.SharedKernel;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Courses.Domain;

public sealed class Course : AggregateRoot<Guid>
{
    private readonly List<Session> _sessions = [];

    public TenantId TenantId { get; private set; } = default!;
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public CourseType CourseType { get; private set; }
    public RegistrationMode RegistrationMode { get; private set; }
    public int Capacity { get; private set; }
    public CourseStatus Status { get; private set; }
    public Guid CreatedByStaffId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public IReadOnlyList<Session> Sessions => _sessions.AsReadOnly();

    private Course() { }

    public static Course Create(
        TenantId tenantId,
        string title,
        string description,
        CourseType courseType,
        RegistrationMode registrationMode,
        int capacity,
        IEnumerable<(DateTimeOffset ScheduledAt, int DurationMinutes, string? Location)> sessionInputs,
        Guid createdByStaffId)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        if (capacity < 1) throw new ArgumentException("Capacity must be at least 1.", nameof(capacity));

        var sessions = sessionInputs
            .OrderBy(s => s.ScheduledAt)
            .Select((s, i) => Session.Create(s.ScheduledAt, s.DurationMinutes, s.Location, i + 1))
            .ToList();

        ValidateSessionCount(courseType, sessions.Count);
        ValidateNoSessionOverlap(sessions);

        var now = DateTimeOffset.UtcNow;
        var course = new Course
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Title = title.Trim(),
            Description = description.Trim(),
            CourseType = courseType,
            RegistrationMode = registrationMode,
            Capacity = capacity,
            Status = CourseStatus.Active,
            CreatedByStaffId = createdByStaffId,
            CreatedAt = now,
            UpdatedAt = now
        };
        course._sessions.AddRange(sessions);

        course.RaiseDomainEvent(new CourseCreated(Guid.NewGuid(), now, course.Id, course.TenantId, course.Title));

        return course;
    }

    public void Update(string? title, string? description, int? capacity, RegistrationMode? registrationMode)
    {
        EnsureEditable();
        if (title is not null) Title = title.Trim();
        if (description is not null) Description = description.Trim();
        if (capacity is not null)
        {
            if (capacity < 1) throw new ArgumentException("Capacity must be at least 1.");
            Capacity = capacity.Value;
        }
        if (registrationMode is not null) RegistrationMode = registrationMode.Value;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        if (Status is CourseStatus.Cancelled or CourseStatus.Completed)
            throw new InvalidOperationException($"Cannot cancel a course with status '{Status}'.");

        Status = CourseStatus.Cancelled;
        UpdatedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new CourseCancelled(Guid.NewGuid(), UpdatedAt, Id, TenantId));
    }

    public void Complete()
    {
        if (Status != CourseStatus.Active)
            throw new InvalidOperationException("Only Active courses can be completed.");
        Status = CourseStatus.Completed;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private void EnsureEditable()
    {
        if (Status is CourseStatus.Cancelled or CourseStatus.Completed)
            throw new InvalidOperationException($"Cannot edit a course with status '{Status}'.");
    }

    private static void ValidateSessionCount(CourseType type, int count)
    {
        if (type == CourseType.OneTime && count != 1)
            throw new ArgumentException("A OneTime course must have exactly 1 session.");
        if (type == CourseType.MultiSession && count < 2)
            throw new ArgumentException("A MultiSession course must have at least 2 sessions.");
    }

    private static void ValidateNoSessionOverlap(List<Session> sessions)
    {
        for (var i = 0; i < sessions.Count - 1; i++)
        {
            if (sessions[i].EndsAt > sessions[i + 1].ScheduledAt)
                throw new ArgumentException($"Session {i + 1} overlaps with session {i + 2}.");
        }
    }
}
