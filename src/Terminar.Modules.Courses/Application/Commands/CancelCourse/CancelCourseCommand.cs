using MediatR;

namespace Terminar.Modules.Courses.Application.Commands.CancelCourse;

public sealed record CancelCourseCommand(Guid CourseId, Guid TenantId) : IRequest;
