namespace Terminar.Modules.Courses.Application.Queries.ExportCourses;

public sealed record ExportCourseRowDto(
    Guid CourseId,
    string Title,
    string Description,
    string CourseType,
    string RegistrationMode,
    int Capacity,
    string Status,
    DateOnly? FirstSessionAt,
    DateOnly? LastSessionEndsAt,
    string? Location);
