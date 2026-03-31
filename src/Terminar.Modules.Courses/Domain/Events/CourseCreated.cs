using Terminar.SharedKernel;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Courses.Domain.Events;

public sealed record CourseCreated(Guid EventId, DateTime OccurredAt, Guid CourseId, TenantId TenantId, string Title) : IDomainEvent;
public sealed record CourseActivated(Guid EventId, DateTime OccurredAt, Guid CourseId, TenantId TenantId) : IDomainEvent;
public sealed record CourseCancelled(Guid EventId, DateTime OccurredAt, Guid CourseId, TenantId TenantId) : IDomainEvent;
