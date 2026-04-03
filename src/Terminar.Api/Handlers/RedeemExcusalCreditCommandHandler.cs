using MediatR;
using Microsoft.EntityFrameworkCore;
using Terminar.Modules.Courses.Infrastructure;
using Terminar.Modules.Registrations.Application.Commands.RedeemExcusalCredit;
using Terminar.Modules.Registrations.Domain;
using Terminar.Modules.Registrations.Domain.Repositories;
using Terminar.Modules.Tenants.Infrastructure;
using Terminar.SharedKernel;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Api.Handlers;

public sealed class RedeemExcusalCreditCommandHandler(
    IExcusalCreditRepository creditRepo,
    IParticipantMagicLinkRepository magicLinkRepo,
    IRegistrationRepository registrationRepo,
    CoursesDbContext coursesDb,
    TenantsDbContext tenantsDb)
    : IRequestHandler<RedeemExcusalCreditCommand, RedeemExcusalCreditResult>
{
    public async Task<RedeemExcusalCreditResult> Handle(RedeemExcusalCreditCommand request, CancellationToken cancellationToken)
    {
        // Validate portal token
        var portalSession = await magicLinkRepo.GetByPortalTokenAsync(request.PortalToken, cancellationToken)
            ?? throw new ForbiddenException("Invalid or expired portal token.");

        if (!portalSession.IsPortalTokenValid)
            throw new ForbiddenException("Portal token has expired.");

        if (portalSession.TenantId.Value != request.TenantId)
            throw new ForbiddenException("Token does not match tenant.");

        // Load credit
        var credit = await creditRepo.GetByIdAsync(request.CreditId, request.TenantId, cancellationToken)
            ?? throw new NotFoundException("Excusal credit not found.");

        if (credit.ParticipantEmail != portalSession.ParticipantEmail.Value)
            throw new ForbiddenException("Credit does not belong to this participant.");

        if (!credit.IsActive)
            throw new UnprocessableException("Excusal credit is not active.");

        // Check validity windows — credit is valid if today falls within any of the valid window ranges
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var validWindows = await tenantsDb.ExcusalValidityWindows
            .Where(w => w.TenantId.Value == request.TenantId && credit.ValidWindowIds.Contains(w.Id))
            .ToListAsync(cancellationToken);

        var isWithinValidWindow = validWindows.Any(w => today >= w.StartDate && today <= w.EndDate);
        if (!isWithinValidWindow)
            throw new UnprocessableException("Excusal credit has expired (no valid window covers today).");

        // Load target course
        var targetCourse = await coursesDb.Courses
            .FirstOrDefaultAsync(c => c.Id == request.TargetCourseId && c.TenantId.Value == request.TenantId, cancellationToken)
            ?? throw new NotFoundException("Target course not found.");

        // Check tag intersection
        var courseTags = targetCourse.ExcusalPolicy.Tags;
        var hasMatchingTag = courseTags.Any(t => credit.Tags.Contains(t));
        if (!hasMatchingTag)
            throw new UnprocessableException("No matching tags between credit and target course.");

        // Check capacity
        var confirmedCount = await registrationRepo.CountConfirmedByCourseAsync(request.TargetCourseId, request.TenantId, cancellationToken);
        if (confirmedCount >= targetCourse.Capacity)
            throw new UnprocessableException("Target course is at capacity.");

        // Create registration
        var participantEmail = Email.From(portalSession.ParticipantEmail.Value);
        var tenantIdVo = TenantId.From(request.TenantId);
        var registration = Registration.Create(tenantIdVo, request.TargetCourseId,
            credit.ParticipantName, participantEmail, RegistrationSource.SelfService, null);

        await registrationRepo.AddAsync(registration, cancellationToken);

        // Redeem credit
        credit.Redeem(request.TargetCourseId);
        await creditRepo.SaveChangesAsync(cancellationToken);

        return new RedeemExcusalCreditResult(registration.Id, registration.SafeLinkToken);
    }
}
