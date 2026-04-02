namespace Terminar.Modules.Courses.Domain;

public sealed class CourseExcusalPolicy
{
    /// <summary>null = use tenant default; true/false = override</summary>
    public bool? CreditGenerationOverride { get; set; }
    public Guid? ValidityWindowId { get; set; }
    public List<string> Tags { get; set; } = [];

    public bool EffectiveCreditGenerationEnabled(bool tenantDefault)
        => CreditGenerationOverride ?? tenantDefault;

    /// <summary>Can generate credits only if effective generation is enabled AND tags exist AND window is assigned.</summary>
    public bool CanGenerateCredits(bool tenantDefault)
        => EffectiveCreditGenerationEnabled(tenantDefault) && Tags.Count > 0 && ValidityWindowId.HasValue;
}
