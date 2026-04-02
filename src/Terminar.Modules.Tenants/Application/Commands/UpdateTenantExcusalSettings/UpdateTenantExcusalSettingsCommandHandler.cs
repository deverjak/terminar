using MediatR;
using Terminar.Modules.Tenants.Domain.Repositories;
using Terminar.SharedKernel;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Tenants.Application.Commands.UpdateTenantExcusalSettings;

public sealed class UpdateTenantExcusalSettingsCommandHandler(ITenantRepository tenantRepo)
    : IRequestHandler<UpdateTenantExcusalSettingsCommand>
{
    public async Task Handle(UpdateTenantExcusalSettingsCommand request, CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var tenant = await tenantRepo.GetByIdAsync(tenantId, cancellationToken)
            ?? throw new NotFoundException("Tenant not found.");

        tenant.UpdateExcusalSettings(
            request.CreditGenerationEnabled,
            request.ForwardWindowCount,
            request.UnenrollmentDeadlineDays,
            request.ExcusalDeadlineHours);

        await tenantRepo.SaveChangesAsync(cancellationToken);
    }
}
