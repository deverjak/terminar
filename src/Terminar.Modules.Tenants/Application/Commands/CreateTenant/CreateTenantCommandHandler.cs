using MediatR;
using Terminar.Modules.Tenants.Domain;
using Terminar.Modules.Tenants.Domain.Repositories;
using Terminar.SharedKernel;

namespace Terminar.Modules.Tenants.Application.Commands.CreateTenant;

public sealed class CreateTenantCommandHandler(ITenantRepository repository)
    : IRequestHandler<CreateTenantCommand, CreateTenantResult>
{
    public async Task<CreateTenantResult> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        if (await repository.ExistsBySlugAsync(request.Slug, cancellationToken))
            throw new ConflictException($"A tenant with slug '{request.Slug}' already exists.");

        var tenant = Tenant.Create(request.Name, request.Slug, request.DefaultLanguageCode);

        await repository.AddAsync(tenant, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return new CreateTenantResult(tenant.Id.Value, tenant.Name, tenant.Slug, tenant.CreatedAt);
    }
}
