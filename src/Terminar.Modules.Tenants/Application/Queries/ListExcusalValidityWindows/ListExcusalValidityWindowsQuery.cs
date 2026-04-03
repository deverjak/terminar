using MediatR;

namespace Terminar.Modules.Tenants.Application.Queries.ListExcusalValidityWindows;

public sealed record ListExcusalValidityWindowsQuery(Guid TenantId) : IRequest<List<ExcusalValidityWindowDto>>;

public sealed record ExcusalValidityWindowDto(Guid WindowId, string Name, DateOnly StartDate, DateOnly EndDate);
