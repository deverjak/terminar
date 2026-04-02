using MediatR;
using Terminar.Modules.Registrations.Domain.Repositories;
using Terminar.SharedKernel;

namespace Terminar.Modules.Registrations.Application.Commands.SoftDeleteExcusalCredit;

public sealed class SoftDeleteExcusalCreditCommandHandler(IExcusalCreditRepository creditRepo)
    : IRequestHandler<SoftDeleteExcusalCreditCommand>
{
    public async Task Handle(SoftDeleteExcusalCreditCommand request, CancellationToken cancellationToken)
    {
        var credit = await creditRepo.GetByIdAsync(request.CreditId, request.TenantId, cancellationToken)
            ?? throw new NotFoundException("Excusal credit not found.");

        credit.SoftDelete(request.ActorStaffId);
        await creditRepo.SaveChangesAsync(cancellationToken);
    }
}
