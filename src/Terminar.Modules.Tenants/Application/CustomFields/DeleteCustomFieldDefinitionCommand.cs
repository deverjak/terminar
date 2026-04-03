using MediatR;
using Terminar.Modules.Tenants.Domain.Events;
using Terminar.Modules.Tenants.Domain.Repositories;
using Terminar.SharedKernel;

namespace Terminar.Modules.Tenants.Application.CustomFields;

public sealed record DeleteCustomFieldDefinitionCommand(Guid Id, Guid TenantId) : IRequest;

public sealed class DeleteCustomFieldDefinitionHandler(ICustomFieldDefinitionRepository repo, IMediator mediator)
    : IRequestHandler<DeleteCustomFieldDefinitionCommand>
{
    public async Task Handle(DeleteCustomFieldDefinitionCommand request, CancellationToken cancellationToken)
    {
        var field = await repo.GetByIdAsync(request.Id, request.TenantId, cancellationToken)
            ?? throw new NotFoundException($"Custom field '{request.Id}' not found.");

        await repo.DeleteAsync(field, cancellationToken);
        await repo.SaveChangesAsync(cancellationToken);

        await mediator.Publish(
            new CustomFieldDefinitionDeleted(Guid.NewGuid(), DateTime.UtcNow, request.Id, request.TenantId),
            cancellationToken);
    }
}
