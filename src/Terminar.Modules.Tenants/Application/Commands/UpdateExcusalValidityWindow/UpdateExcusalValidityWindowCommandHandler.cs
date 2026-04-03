using MediatR;
using Terminar.Modules.Tenants.Domain.Repositories;
using Terminar.SharedKernel;

namespace Terminar.Modules.Tenants.Application.Commands.UpdateExcusalValidityWindow;

public sealed class UpdateExcusalValidityWindowCommandHandler(IExcusalValidityWindowRepository repo)
    : IRequestHandler<UpdateExcusalValidityWindowCommand>
{
    public async Task Handle(UpdateExcusalValidityWindowCommand request, CancellationToken cancellationToken)
    {
        var window = await repo.GetByIdAsync(request.WindowId, request.TenantId, cancellationToken)
            ?? throw new NotFoundException("Validity window not found.");

        if (request.Name is not null && request.Name != window.Name)
        {
            if (await repo.ExistsByNameAsync(request.TenantId, request.Name, request.WindowId, cancellationToken))
                throw new ConflictException($"A window named '{request.Name}' already exists.");
        }

        window.Update(request.Name, request.StartDate, request.EndDate);
        await repo.SaveChangesAsync(cancellationToken);
    }
}
