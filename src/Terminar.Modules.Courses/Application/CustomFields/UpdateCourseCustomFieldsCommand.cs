using FluentValidation;
using MediatR;
using Terminar.Modules.Courses.Domain.Repositories;
using Terminar.Modules.Tenants.Application.CustomFields;
using Terminar.SharedKernel;

namespace Terminar.Modules.Courses.Application.CustomFields;

public sealed record UpdateCourseCustomFieldsCommand(
    Guid CourseId,
    Guid TenantId,
    List<Guid> EnabledFieldIds) : IRequest;

public sealed class UpdateCourseCustomFieldsValidator : AbstractValidator<UpdateCourseCustomFieldsCommand>
{
    public UpdateCourseCustomFieldsValidator()
    {
        RuleFor(x => x.EnabledFieldIds).NotNull();
    }
}

public sealed class UpdateCourseCustomFieldsHandler(
    ICourseRepository courseRepo,
    IMediator mediator) : IRequestHandler<UpdateCourseCustomFieldsCommand>
{
    public async Task Handle(UpdateCourseCustomFieldsCommand request, CancellationToken cancellationToken)
    {
        // Validate all field IDs belong to the tenant
        if (request.EnabledFieldIds.Count > 0)
        {
            var tenantFields = await mediator.Send(
                new ListCustomFieldDefinitionsQuery(request.TenantId), cancellationToken);
            var tenantFieldIds = tenantFields.Select(f => f.Id).ToHashSet();

            var invalidIds = request.EnabledFieldIds.Where(id => !tenantFieldIds.Contains(id)).ToList();
            if (invalidIds.Count > 0)
                throw new UnprocessableException(
                    $"Field ID(s) {string.Join(", ", invalidIds)} do not belong to this tenant.");
        }

        var course = await courseRepo.GetByIdAsync(request.CourseId)
            ?? throw new NotFoundException($"Course '{request.CourseId}' not found.");

        if (course.TenantId.Value != request.TenantId)
            throw new NotFoundException($"Course '{request.CourseId}' not found.");

        course.SetCustomFieldAssignments(request.EnabledFieldIds);
        await courseRepo.UpdateAsync(course);
    }
}
