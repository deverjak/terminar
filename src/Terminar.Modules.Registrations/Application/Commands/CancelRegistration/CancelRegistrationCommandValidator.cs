using FluentValidation;

namespace Terminar.Modules.Registrations.Application.Commands.CancelRegistration;

public sealed class CancelRegistrationCommandValidator : AbstractValidator<CancelRegistrationCommand>
{
    public CancelRegistrationCommandValidator()
    {
        RuleFor(x => x.RegistrationId).NotEmpty();
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.TenantId).NotEmpty();

        RuleFor(x => x)
            .Must(x => x.SelfCancellationToken.HasValue || x.StaffUserId.HasValue)
            .WithMessage("Either a cancellation token or staff authentication is required.");
    }
}
