using MediatR;
using Terminar.Modules.Tenants.Domain.Repositories;
using Terminar.SharedKernel;

namespace Terminar.Modules.Tenants.Application.Commands.DeleteExcusalValidityWindow;

public sealed class DeleteExcusalValidityWindowCommandHandler(IExcusalValidityWindowRepository repo)
    : IRequestHandler<DeleteExcusalValidityWindowCommand>
{
    public async Task Handle(DeleteExcusalValidityWindowCommand request, CancellationToken cancellationToken)
    {
        var window = await repo.GetByIdAsync(request.WindowId, request.TenantId, cancellationToken)
            ?? throw new NotFoundException("Validity window not found.");

        window.SoftDelete();
        await repo.SaveChangesAsync(cancellationToken);
    }
}
