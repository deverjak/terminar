using MediatR;
using Microsoft.EntityFrameworkCore;
using Terminar.Modules.Courses.Application.Queries.GetCourseExcusalPolicy;
using Terminar.Modules.Courses.Infrastructure;
using Terminar.Modules.Tenants.Infrastructure;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Api.Handlers;

public sealed class GetCourseExcusalPolicyHandler(
    CoursesDbContext coursesDb,
    TenantsDbContext tenantsDb)
    : IRequestHandler<GetCourseExcusalPolicyQuery, CourseExcusalPolicyDto?>
{
    public async Task<CourseExcusalPolicyDto?> Handle(GetCourseExcusalPolicyQuery request, CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);

        var course = await coursesDb.Courses
            .FirstOrDefaultAsync(c => c.Id == request.CourseId && c.TenantId == tenantId, cancellationToken);
        if (course is null) return null;

        var tenant = await tenantsDb.Tenants
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
        var tenantDefault = tenant?.ExcusalSettings.CreditGenerationEnabled ?? false;

        var policy = course.ExcusalPolicy;
        return new CourseExcusalPolicyDto(
            course.Id,
            policy.CreditGenerationOverride,
            policy.ValidityWindowId,
            policy.Tags,
            policy.EffectiveCreditGenerationEnabled(tenantDefault));
    }
}
