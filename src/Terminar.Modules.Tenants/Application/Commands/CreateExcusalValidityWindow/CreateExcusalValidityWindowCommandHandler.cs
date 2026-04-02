using MediatR;
using Terminar.Modules.Tenants.Domain;
using Terminar.Modules.Tenants.Domain.Repositories;
using Terminar.SharedKernel;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Tenants.Application.Commands.CreateExcusalValidityWindow;

public sealed class CreateExcusalValidityWindowCommandHandler(IExcusalValidityWindowRepository repo)
    : IRequestHandler<CreateExcusalValidityWindowCommand, Guid>
{
    public async Task<Guid> Handle(CreateExcusalValidityWindowCommand request, CancellationToken cancellationToken)
    {
        if (await repo.ExistsByNameAsync(request.TenantId, request.Name, ct: cancellationToken))
            throw new ConflictException($"A validity window named '{request.Name}' already exists.");

        var tenantId = TenantId.From(request.TenantId);
        var window = ExcusalValidityWindow.Create(tenantId, request.Name, request.StartDate, request.EndDate);
        await repo.AddAsync(window, cancellationToken);
        await repo.SaveChangesAsync(cancellationToken);
        return window.Id;
    }
}
