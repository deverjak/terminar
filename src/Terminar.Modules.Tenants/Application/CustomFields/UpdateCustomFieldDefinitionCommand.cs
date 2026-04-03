using FluentValidation;
using MediatR;
using Terminar.Modules.Tenants.Domain.Repositories;
using Terminar.SharedKernel;

namespace Terminar.Modules.Tenants.Application.CustomFields;

public sealed record UpdateCustomFieldDefinitionCommand(
    Guid Id,
    Guid TenantId,
    string? Name,
    List<string>? AllowedValues) : IRequest;

public sealed class UpdateCustomFieldDefinitionValidator : AbstractValidator<UpdateCustomFieldDefinitionCommand>
{
    public UpdateCustomFieldDefinitionValidator()
    {
        When(x => x.Name is not null, () =>
            RuleFor(x => x.Name!).NotEmpty().MaximumLength(100));
    }
}

public sealed class UpdateCustomFieldDefinitionHandler(ICustomFieldDefinitionRepository repo)
    : IRequestHandler<UpdateCustomFieldDefinitionCommand>
{
    public async Task Handle(UpdateCustomFieldDefinitionCommand request, CancellationToken cancellationToken)
    {
        var field = await repo.GetByIdAsync(request.Id, request.TenantId, cancellationToken)
            ?? throw new NotFoundException($"Custom field '{request.Id}' not found.");

        if (request.Name is not null)
        {
            if (await repo.ExistsByNameAsync(request.TenantId, request.Name, excludeId: request.Id, ct: cancellationToken))
                throw new ConflictException($"A custom field named '{request.Name}' already exists.");
            field.UpdateName(request.Name);
        }

        if (request.AllowedValues is not null)
            field.UpdateAllowedValues(request.AllowedValues);

        await repo.SaveChangesAsync(cancellationToken);
    }
}
