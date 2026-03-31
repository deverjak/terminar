using FluentValidation;

namespace Terminar.Modules.Courses.Application.Commands.CreateCourse;

public sealed class CreateCourseValidator : AbstractValidator<CreateCourseCommand>
{
    public CreateCourseValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Capacity).GreaterThan(0);
        RuleFor(x => x.Sessions).NotEmpty();
        RuleFor(x => x.CreatedByStaffId).NotEmpty();

        RuleForEach(x => x.Sessions).ChildRules(s =>
        {
            s.RuleFor(x => x.DurationMinutes).GreaterThan(0);
            s.RuleFor(x => x.ScheduledAt).GreaterThan(DateTimeOffset.UtcNow);
        });
    }
}
