using MediatR;
using Terminar.Modules.Registrations.Domain.Repositories;
using Terminar.SharedKernel;

namespace Terminar.Modules.Registrations.Application.Commands.UpdateExcusalCredit;

public sealed class UpdateExcusalCreditCommandHandler(IExcusalCreditRepository creditRepo)
    : IRequestHandler<UpdateExcusalCreditCommand>
{
    public async Task Handle(UpdateExcusalCreditCommand request, CancellationToken cancellationToken)
    {
        var credit = await creditRepo.GetByIdAsync(request.CreditId, request.TenantId, cancellationToken)
            ?? throw new NotFoundException("Excusal credit not found.");

        if (request.AdditionalWindowIds is { Count: > 0 })
            credit.Extend(request.AdditionalWindowIds, request.ActorStaffId);

        if (request.NewTags is not null)
            credit.ReTag(request.NewTags, request.ActorStaffId);

        await creditRepo.SaveChangesAsync(cancellationToken);
    }
}
