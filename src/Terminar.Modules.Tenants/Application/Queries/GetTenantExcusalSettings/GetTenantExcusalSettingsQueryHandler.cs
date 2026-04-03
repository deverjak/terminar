using MediatR;
using Terminar.Modules.Tenants.Domain.Repositories;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Tenants.Application.Queries.GetTenantExcusalSettings;

public sealed class GetTenantExcusalSettingsQueryHandler(ITenantRepository tenantRepo)
    : IRequestHandler<GetTenantExcusalSettingsQuery, TenantExcusalSettingsDto?>
{
    public async Task<TenantExcusalSettingsDto?> Handle(GetTenantExcusalSettingsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var tenant = await tenantRepo.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null) return null;

        var s = tenant.ExcusalSettings;
        return new TenantExcusalSettingsDto(s.CreditGenerationEnabled, s.ForwardWindowCount, s.UnenrollmentDeadlineDays, s.ExcusalDeadlineHours);
    }
}
