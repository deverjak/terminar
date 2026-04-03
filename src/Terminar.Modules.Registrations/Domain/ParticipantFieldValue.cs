using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Registrations.Domain;

/// <summary>
/// Stores the value of a custom field for a specific enrollment (Registration).
/// FieldDefinitionId is a cross-module reference by value — no FK constraint across schemas.
/// Value serialization: YesNo → "true"/"false"/null, Text → raw string/null, OptionsList → selected string/null.
/// </summary>
public sealed class ParticipantFieldValue
{
    public Guid Id { get; private set; }
    public Guid RegistrationId { get; private set; }
    public TenantId TenantId { get; private set; } = default!;

    /// <summary>Cross-module reference to tenants.custom_field_definitions.id</summary>
    public Guid FieldDefinitionId { get; private set; }

    public string? Value { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private ParticipantFieldValue() { }

    internal static ParticipantFieldValue Create(
        Guid registrationId,
        TenantId tenantId,
        Guid fieldDefinitionId,
        string? value) =>
        new()
        {
            Id = Guid.NewGuid(),
            RegistrationId = registrationId,
            TenantId = tenantId,
            FieldDefinitionId = fieldDefinitionId,
            Value = value,
            UpdatedAt = DateTimeOffset.UtcNow
        };

    internal void SetValue(string? value)
    {
        Value = value;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
