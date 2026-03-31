using FluentValidation;

namespace Terminar.Modules.Identity.Application.Commands.CreateStaffUser;

public sealed class CreateStaffUserCommandValidator : AbstractValidator<CreateStaffUserCommand>
{
    public CreateStaffUserCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.Role).NotEmpty().Must(r => r is "Staff" or "Admin")
            .WithMessage("Role must be 'Staff' or 'Admin'.");
    }
}
