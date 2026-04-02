using MediatR;

namespace Terminar.Modules.Courses.Application.Commands.UpdateCourseExcusalPolicy;

public sealed record UpdateCourseExcusalPolicyCommand(
    Guid CourseId,
    Guid TenantId,
    bool? CreditGenerationOverride,
    bool ClearOverride,
    Guid? ValidityWindowId,
    bool ClearWindow,
    List<string>? Tags) : IRequest;
