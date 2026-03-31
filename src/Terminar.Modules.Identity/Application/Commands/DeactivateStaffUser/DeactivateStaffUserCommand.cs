using MediatR;

namespace Terminar.Modules.Identity.Application.Commands.DeactivateStaffUser;

public sealed record DeactivateStaffUserCommand(Guid StaffUserId) : IRequest;
