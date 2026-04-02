using MediatR;
using Microsoft.EntityFrameworkCore;
using Terminar.Modules.Courses.Infrastructure;
using Terminar.Modules.Registrations.Domain;
using Terminar.Modules.Registrations.Domain.Events;
using Terminar.Modules.Registrations.Domain.Repositories;
using Terminar.Modules.Tenants.Infrastructure;

namespace Terminar.Api.Notifications;

public sealed class ExcusalCreatedCreditHandler(
    IExcusalRepository excusalRepo,
    IExcusalCreditRepository creditRepo,
    CoursesDbContext coursesDb,
    TenantsDbContext tenantsDb,
    ILogger<ExcusalCreatedCreditHandler> logger)
    : INotificationHandler<ExcusalCreated>
{
    public async Task Handle(ExcusalCreated notification, CancellationToken cancellationToken)
    {
        try
        {
            var tenantRecord = await tenantsDb.Tenants
                .FirstOrDefaultAsync(t => t.Id.Value == notification.TenantId.Value, cancellationToken);
            if (tenantRecord is null) return;

            var course = await coursesDb.Courses
                .FirstOrDefaultAsync(c => c.Id == notification.CourseId, cancellationToken);
            if (course is null) return;

            var policy = course.ExcusalPolicy;
            var tenantSettings = tenantRecord.ExcusalSettings;

            // Check if credit generation is enabled
            if (!policy.CanGenerateCredits(tenantSettings.CreditGenerationEnabled))
                return;

            // Get validity windows
            var sourceWindowId = policy.ValidityWindowId!.Value;
            var allWindows = await tenantsDb.ExcusalValidityWindows
                .Where(w => w.TenantId.Value == notification.TenantId.Value)
                .OrderBy(w => w.StartDate)
                .ToListAsync(cancellationToken);

            var sourceIndex = allWindows.FindIndex(w => w.Id == sourceWindowId);
            if (sourceIndex < 0) return;

            var forwardCount = tenantSettings.ForwardWindowCount;
            var validWindowIds = allWindows
                .Skip(sourceIndex)
                .Take(forwardCount + 1)
                .Select(w => w.Id)
                .ToList();

            var credit = ExcusalCredit.Issue(
                notification.TenantId,
                notification.ParticipantEmail,
                notification.ParticipantName,
                notification.ExcusalId,
                notification.CourseId,
                notification.SessionId,
                policy.Tags,
                validWindowIds);

            await creditRepo.AddAsync(credit, cancellationToken);

            // Update excusal status
            var excusal = await excusalRepo.GetByIdAsync(notification.ExcusalId, notification.TenantId.Value, cancellationToken);
            if (excusal is not null)
            {
                excusal.MarkCreditIssued(credit.Id);
                await excusalRepo.UpdateAsync(excusal, cancellationToken);
            }

            await creditRepo.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Issued excusal credit {CreditId} for excusal {ExcusalId}", credit.Id, notification.ExcusalId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to issue excusal credit for excusal {ExcusalId}", notification.ExcusalId);
        }
    }
}
