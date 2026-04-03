namespace Terminar.Modules.Tenants.Domain;

public sealed class TenantExcusalSettings
{
    public bool CreditGenerationEnabled { get; set; } = false;
    public int ForwardWindowCount { get; set; } = 2;
    public int UnenrollmentDeadlineDays { get; set; } = 14;
    public int ExcusalDeadlineHours { get; set; } = 24;

    public static TenantExcusalSettings Default() => new();
}
