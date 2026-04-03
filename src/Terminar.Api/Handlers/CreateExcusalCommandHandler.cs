using MediatR;
using Microsoft.EntityFrameworkCore;
using Terminar.Modules.Courses.Infrastructure;
using Terminar.Modules.Registrations.Application.Commands.CreateExcusal;
using Terminar.Modules.Registrations.Domain;
using Terminar.Modules.Registrations.Domain.Repositories;
using Terminar.Modules.Tenants.Infrastructure;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Api.Handlers;

public sealed class CreateExcusalCommandHandler(
    IRegistrationRepository registrationRepo,
    IExcusalRepository excusalRepo,
    CoursesDbContext coursesDb,
    TenantsDbContext tenantsDb)
    : IRequestHandler<CreateExcusalCommand, CreateExcusalResult>
{
    public async Task<CreateExcusalResult> Handle(CreateExcusalCommand request, CancellationToken cancellationToken)
    {
        var registration = await registrationRepo.GetBySafeLinkTokenAsync(
            request.SafeLinkToken, request.TenantId, cancellationToken)
            ?? throw new Terminar.SharedKernel.NotFoundException("Enrollment not found.");

        if (registration.Status == RegistrationStatus.Cancelled)
            throw new Terminar.SharedKernel.UnprocessableException("Enrollment is cancelled.");

        var course = await coursesDb.Courses
            .Include(c => c.Sessions)
            .FirstOrDefaultAsync(c => c.Id == registration.CourseId, cancellationToken)
            ?? throw new Terminar.SharedKernel.NotFoundException("Course not found.");

        var session = course.Sessions.FirstOrDefault(s => s.Id == request.SessionId)
            ?? throw new Terminar.SharedKernel.NotFoundException("Session not found.");

        var tenantId = TenantId.From(request.TenantId);
        var tenantRecord = await tenantsDb.Tenants
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
        var deadlineHours = tenantRecord?.ExcusalSettings.ExcusalDeadlineHours ?? 24;
        var deadline = session.ScheduledAt.AddHours(-deadlineHours);
        var now = DateTime.UtcNow;

        if (now > session.ScheduledAt)
            throw new Terminar.SharedKernel.UnprocessableException("Cannot excuse from a past session.");

        if (now >= deadline)
            throw new Terminar.SharedKernel.UnprocessableException("Excusal deadline has passed.");

        var isDuplicate = await excusalRepo.ExistsForSessionAsync(registration.Id, request.SessionId, cancellationToken);
        if (isDuplicate)
            throw new Terminar.SharedKernel.ConflictException("Already excused from this session.");

        var excusal = Excusal.Create(tenantId, registration.Id, registration.CourseId, request.SessionId,
            registration.ParticipantEmail.Value, registration.ParticipantName);

        await excusalRepo.AddAsync(excusal, cancellationToken);
        await excusalRepo.SaveChangesAsync(cancellationToken);

        return new CreateExcusalResult(excusal.Id, excusal.Status == ExcusalStatus.CreditIssued);
    }
}
