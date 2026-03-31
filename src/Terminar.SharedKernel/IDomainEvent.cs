using MediatR;

namespace Terminar.SharedKernel;

public interface IDomainEvent : INotification
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
}
