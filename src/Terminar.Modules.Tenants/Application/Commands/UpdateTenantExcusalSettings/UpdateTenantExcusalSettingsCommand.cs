using MediatR;

namespace Terminar.Modules.Tenants.Application.Commands.UpdateTenantExcusalSettings;

public sealed record UpdateTenantExcusalSettingsCommand(
    Guid TenantId,
    bool? CreditGenerationEnabled,
    int? ForwardWindowCount,
    int? UnenrollmentDeadlineDays,
    int? ExcusalDeadlineHours) : IRequest;
