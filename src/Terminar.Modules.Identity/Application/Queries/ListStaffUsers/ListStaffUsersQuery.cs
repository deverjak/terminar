using MediatR;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Identity.Application.Queries.ListStaffUsers;

public sealed record ListStaffUsersQuery(TenantId TenantId) : IRequest<IReadOnlyList<StaffUserDto>>;

public sealed record StaffUserDto(Guid StaffUserId, string Username, string Email, string Role, string Status, DateTimeOffset CreatedAt);
