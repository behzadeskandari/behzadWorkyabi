using IranJob.SharedKernel.Events;

namespace IranJob.BuildingBlocks.Infrastructure.Events;

/// <summary>
/// Dispatches domain events to in-process handlers resolved from the DI container.
/// Keeps domain modules decoupled (no module references another except through
/// abstractions); the dispatcher is the only cross-cutting infrastructure dependency.
/// </summary>
public interface IEventDispatcher
{
    Task DispatchAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent;

    /// <summary>
    /// Dispatches a domain event whose concrete type is only known at runtime,
    /// without relying on <c>dynamic</c> (DLR binding). Handlers are resolved by
    /// the event's runtime type, keeping the call site statically verifiable.
    /// </summary>
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
