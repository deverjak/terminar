using MediatR;
using Terminar.Modules.Tenants.Domain.Repositories;
using Terminar.SharedKernel;

namespace Terminar.Modules.Tenants.Application.Queries.GetTenant;

public sealed class GetTenantQueryHandler(ITenantRepository repository)
    : IRequestHandler<GetTenantQuery, GetTenantResult>
{
    public async Task<GetTenantResult> Handle(GetTenantQuery request, CancellationToken cancellationToken)
    {
        var tenant = await repository.GetByIdAsync(request.TenantId, cancellationToken)
            ?? throw new NotFoundException($"Tenant '{request.TenantId}' not found.");

        return new GetTenantResult(
            tenant.Id.Value,
            tenant.Name,
            tenant.Slug,
            tenant.DefaultLanguageCode,
            tenant.Status.ToString(),
            tenant.CreatedAt);
    }
}
