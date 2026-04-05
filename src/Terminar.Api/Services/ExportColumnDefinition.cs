namespace Terminar.Api.Services;

public enum ExportColumnGroup
{
    CourseInfo,
    ParticipantInfo,
    CustomFields
}

public sealed record ExportColumnDefinition(
    string Key,
    string LabelKey,
    ExportColumnGroup Group,
    bool DefaultEnabled,
    bool RequiresParticipants,
    string? Label = null);
