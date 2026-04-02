using MediatR;

namespace Terminar.Modules.Tenants.Application.Queries.GetTenantExcusalSettings;

public sealed record GetTenantExcusalSettingsQuery(Guid TenantId) : IRequest<TenantExcusalSettingsDto?>;

public sealed record TenantExcusalSettingsDto(
    bool CreditGenerationEnabled,
    int ForwardWindowCount,
    int UnenrollmentDeadlineDays,
    int ExcusalDeadlineHours);
