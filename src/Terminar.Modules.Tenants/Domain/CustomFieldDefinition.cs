using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Tenants.Domain;

public sealed class CustomFieldDefinition
{
    public Guid Id { get; private set; }
    public TenantId TenantId { get; private set; } = default!;
    public string Name { get; private set; } = string.Empty;
    public CustomFieldType FieldType { get; private set; }
    public List<string> AllowedValues { get; private set; } = [];
    public int DisplayOrder { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private CustomFieldDefinition() { }

    public static CustomFieldDefinition Create(
        TenantId tenantId,
        string name,
        CustomFieldType fieldType,
        List<string> allowedValues,
        int displayOrder)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.Length > 100)
            throw new ArgumentException("Field name must not exceed 100 characters.", nameof(name));

        if (fieldType == CustomFieldType.OptionsList && (allowedValues is null || allowedValues.Count == 0))
            throw new ArgumentException("OptionsList fields must have at least one allowed value.", nameof(allowedValues));

        if (fieldType != CustomFieldType.OptionsList && allowedValues is { Count: > 0 })
            throw new ArgumentException("AllowedValues must be empty for non-OptionsList fields.", nameof(allowedValues));

        return new CustomFieldDefinition
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name.Trim(),
            FieldType = fieldType,
            AllowedValues = allowedValues ?? [],
            DisplayOrder = displayOrder,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void UpdateName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.Length > 100)
            throw new ArgumentException("Field name must not exceed 100 characters.", nameof(name));
        Name = name.Trim();
    }

    public void UpdateAllowedValues(List<string> allowedValues)
    {
        if (FieldType == CustomFieldType.OptionsList && (allowedValues is null || allowedValues.Count == 0))
            throw new ArgumentException("OptionsList fields must have at least one allowed value.", nameof(allowedValues));
        if (FieldType != CustomFieldType.OptionsList && allowedValues is { Count: > 0 })
            throw new ArgumentException("AllowedValues must be empty for non-OptionsList fields.", nameof(allowedValues));
        AllowedValues = allowedValues ?? [];
    }
}
