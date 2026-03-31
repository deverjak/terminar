using MediatR;
using Terminar.Modules.Courses.Domain;

namespace Terminar.Modules.Courses.Application.Commands.UpdateCourse;

public sealed record UpdateCourseCommand(
    Guid CourseId,
    Guid TenantId,
    string? Title,
    string? Description,
    int? Capacity,
    RegistrationMode? RegistrationMode) : IRequest;
