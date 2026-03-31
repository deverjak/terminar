using MediatR;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Identity.Application.Commands.CreateStaffUser;

public sealed record CreateStaffUserCommand(
    TenantId TenantId,
    string Username,
    string Email,
    string Password,
    string Role) : IRequest<CreateStaffUserResult>;

public sealed record CreateStaffUserResult(Guid StaffUserId, string Username, string Email, string Role, DateTime CreatedAt);
