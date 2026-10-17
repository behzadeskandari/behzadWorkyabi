using IranJob.SharedKernel.Events;

namespace IranJob.BuildingBlocks.Infrastructure.Events;

/// <summary>
/// Handles a specific domain event type. Implementations are registered
/// as scoped services so they can use EF Core DbContexts.
/// </summary>
/// <typeparam name="TEvent">The domain event type.</typeparam>
public interface IEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);
}
