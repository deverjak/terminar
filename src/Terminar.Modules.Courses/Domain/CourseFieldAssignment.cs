namespace Terminar.Modules.Courses.Domain;

/// <summary>
/// Records that a specific CustomFieldDefinition (from the Tenants module) is enabled for a Course.
/// FieldDefinitionId is a cross-module reference by value — no FK constraint across schemas.
/// </summary>
public sealed class CourseFieldAssignment
{
    public Guid Id { get; private set; }
    public Guid CourseId { get; private set; }

    /// <summary>Cross-module reference to tenants.custom_field_definitions.id</summary>
    public Guid FieldDefinitionId { get; private set; }

    public int DisplayOrder { get; private set; }

    private CourseFieldAssignment() { }

    internal static CourseFieldAssignment Create(Guid courseId, Guid fieldDefinitionId, int displayOrder) =>
        new()
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            FieldDefinitionId = fieldDefinitionId,
            DisplayOrder = displayOrder
        };
}
