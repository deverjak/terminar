using MediatR;

namespace Terminar.Modules.Courses.Application.Queries.GetCourseExcusalPolicy;

public sealed record GetCourseExcusalPolicyQuery(Guid CourseId, Guid TenantId)
    : IRequest<CourseExcusalPolicyDto?>;

public sealed record CourseExcusalPolicyDto(
    Guid CourseId,
    bool? CreditGenerationOverride,
    Guid? ValidityWindowId,
    List<string> Tags,
    bool EffectiveCreditGenerationEnabled);
