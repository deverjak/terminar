using MediatR;
using Terminar.Modules.Tenants.Domain.Repositories;

namespace Terminar.Modules.Tenants.Application.Queries.ListExcusalValidityWindows;

public sealed class ListExcusalValidityWindowsQueryHandler(IExcusalValidityWindowRepository repo)
    : IRequestHandler<ListExcusalValidityWindowsQuery, List<ExcusalValidityWindowDto>>
{
    public async Task<List<ExcusalValidityWindowDto>> Handle(ListExcusalValidityWindowsQuery request, CancellationToken cancellationToken)
    {
        var windows = await repo.ListByTenantAsync(request.TenantId, cancellationToken);
        return windows.Select(w => new ExcusalValidityWindowDto(w.Id, w.Name, w.StartDate, w.EndDate)).ToList();
    }
}
