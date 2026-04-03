using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Terminar.Modules.Courses.Application.CustomFields;
using Terminar.Modules.Registrations.Infrastructure;
using Terminar.SharedKernel;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Registrations.Application.CustomFields;

public sealed record SetParticipantFieldValueCommand(
    Guid CourseId,
    Guid RegistrationId,
    Guid TenantId,
    Guid FieldDefinitionId,
    string? Value) : IRequest;

public sealed class SetParticipantFieldValueValidator : AbstractValidator<SetParticipantFieldValueCommand>
{
    public SetParticipantFieldValueValidator()
    {
        RuleFor(x => x.FieldDefinitionId).NotEmpty();
    }
}

public sealed class SetParticipantFieldValueHandler(
    RegistrationsDbContext db,
    IMediator mediator) : IRequestHandler<SetParticipantFieldValueCommand>
{
    public async Task Handle(SetParticipantFieldValueCommand request, CancellationToken cancellationToken)
    {
        var tid = TenantId.From(request.TenantId);

        // Verify the field is enabled for the course
        var courseFields = await mediator.Send(
            new GetCourseCustomFieldsQuery(request.CourseId, request.TenantId), cancellationToken);

        var fieldDef = courseFields.FirstOrDefault(f => f.FieldDefinitionId == request.FieldDefinitionId)
            ?? throw new UnprocessableException(
                $"Field '{request.FieldDefinitionId}' is not enabled for course '{request.CourseId}'.");

        if (!fieldDef.IsEnabled)
            throw new UnprocessableException(
                $"Field '{request.FieldDefinitionId}' is not enabled for course '{request.CourseId}'.");

        // Validate value for OptionsList type
        if (fieldDef.FieldType == "OptionsList"
            && request.Value is not null
            && !fieldDef.AllowedValues.Contains(request.Value))
        {
            throw new UnprocessableException(
                $"'{request.Value}' is not a valid option. Allowed: {string.Join(", ", fieldDef.AllowedValues)}");
        }

        // Load the registration (with field values)
        var registration = await db.Registrations
            .Include(r => r.FieldValues)
            .FirstOrDefaultAsync(
                r => r.Id == request.RegistrationId && r.TenantId == tid && r.CourseId == request.CourseId,
                cancellationToken)
            ?? throw new NotFoundException($"Registration '{request.RegistrationId}' not found.");

        registration.SetFieldValue(request.FieldDefinitionId, request.Value);
        await db.SaveChangesAsync(cancellationToken);
    }
}
