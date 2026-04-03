using MediatR;
using Terminar.Modules.Tenants.Domain.Repositories;

namespace Terminar.Modules.Tenants.Application.CustomFields;

public sealed record ListCustomFieldDefinitionsQuery(Guid TenantId) : IRequest<List<CustomFieldDefinitionDto>>;

public sealed record CustomFieldDefinitionDto(
    Guid Id,
    string Name,
    string FieldType,
    List<string> AllowedValues,
    int DisplayOrder);

public sealed class ListCustomFieldDefinitionsHandler(ICustomFieldDefinitionRepository repo)
    : IRequestHandler<ListCustomFieldDefinitionsQuery, List<CustomFieldDefinitionDto>>
{
    public async Task<List<CustomFieldDefinitionDto>> Handle(
        ListCustomFieldDefinitionsQuery request,
        CancellationToken cancellationToken)
    {
        var fields = await repo.ListByTenantAsync(request.TenantId, cancellationToken);
        return fields.Select(f => new CustomFieldDefinitionDto(
            f.Id,
            f.Name,
            f.FieldType.ToString(),
            f.AllowedValues,
            f.DisplayOrder)).ToList();
    }
}
