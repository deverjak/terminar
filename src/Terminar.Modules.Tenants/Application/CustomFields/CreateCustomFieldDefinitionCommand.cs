using FluentValidation;
using MediatR;
using Terminar.Modules.Tenants.Domain;
using Terminar.Modules.Tenants.Domain.Repositories;
using Terminar.SharedKernel;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Tenants.Application.CustomFields;

public sealed record CreateCustomFieldDefinitionCommand(
    Guid TenantId,
    string Name,
    string FieldType,
    List<string> AllowedValues) : IRequest<Guid>;

public sealed class CreateCustomFieldDefinitionValidator : AbstractValidator<CreateCustomFieldDefinitionCommand>
{
    public CreateCustomFieldDefinitionValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.FieldType)
            .Must(t => Enum.TryParse<CustomFieldType>(t, out _))
            .WithMessage("FieldType must be one of: YesNo, Text, OptionsList.");
        RuleFor(x => x.AllowedValues)
            .NotNull()
            .Must((cmd, values) =>
                cmd.FieldType != CustomFieldType.OptionsList.ToString() || values.Count > 0)
            .WithMessage("AllowedValues must have at least one entry for OptionsList fields.");
        RuleFor(x => x.AllowedValues)
            .Must((cmd, values) =>
                cmd.FieldType == CustomFieldType.OptionsList.ToString() || values.Count == 0)
            .WithMessage("AllowedValues must be empty for non-OptionsList fields.");
    }
}

public sealed class CreateCustomFieldDefinitionHandler(ICustomFieldDefinitionRepository repo)
    : IRequestHandler<CreateCustomFieldDefinitionCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateCustomFieldDefinitionCommand request,
        CancellationToken cancellationToken)
    {
        if (await repo.ExistsByNameAsync(request.TenantId, request.Name, ct: cancellationToken))
            throw new ConflictException($"A custom field named '{request.Name}' already exists.");

        var tenantId = TenantId.From(request.TenantId);
        var fieldType = Enum.Parse<CustomFieldType>(request.FieldType);

        var existing = await repo.ListByTenantAsync(request.TenantId, cancellationToken);
        var displayOrder = existing.Count;

        var field = CustomFieldDefinition.Create(tenantId, request.Name, fieldType, request.AllowedValues, displayOrder);
        await repo.AddAsync(field, cancellationToken);
        await repo.SaveChangesAsync(cancellationToken);
        return field.Id;
    }
}
