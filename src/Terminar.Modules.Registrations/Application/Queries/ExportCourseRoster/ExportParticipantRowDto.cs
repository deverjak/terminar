namespace Terminar.Modules.Registrations.Application.Queries.ExportCourseRoster;

public sealed record ExportParticipantRowDto(
    Guid CourseId,
    string ParticipantName,
    string ParticipantEmail,
    string EnrollmentStatus,
    DateOnly EnrollmentDate,
    Dictionary<Guid, string?> CustomFieldValues,
    int? ExcusalCount);
