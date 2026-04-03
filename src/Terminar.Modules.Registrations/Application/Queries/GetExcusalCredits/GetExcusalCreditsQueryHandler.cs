using MediatR;
using Terminar.Modules.Registrations.Domain.Repositories;

namespace Terminar.Modules.Registrations.Application.Queries.GetExcusalCredits;

public sealed class GetExcusalCreditsQueryHandler(IExcusalCreditRepository creditRepo)
    : IRequestHandler<GetExcusalCreditsQuery, GetExcusalCreditsResult>
{
    public async Task<GetExcusalCreditsResult> Handle(GetExcusalCreditsQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await creditRepo.ListByTenantAsync(
            request.TenantId, request.Status, request.ParticipantEmail, request.Page, request.PageSize, cancellationToken);

        var dtos = items.Select(c => new ExcusalCreditDto(
            c.Id, c.ParticipantEmail, c.ParticipantName, c.SourceCourseId, c.SourceSessionId,
            c.Tags, c.ValidWindowIds, c.Status.ToString(), c.CreatedAt, c.DeletedAt,
            c.AuditEntries.Select(a => new AuditEntryDto(a.ActorStaffId, a.ActionType.ToString(),
                a.FieldChanged, a.PreviousValue, a.NewValue, a.Timestamp)).ToList()
        )).ToList();

        return new GetExcusalCreditsResult(dtos, total, request.Page, request.PageSize);
    }
}
