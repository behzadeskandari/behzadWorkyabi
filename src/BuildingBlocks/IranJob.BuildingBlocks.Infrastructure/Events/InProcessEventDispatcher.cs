using System;
using System.Collections;
using IranJob.SharedKernel.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IranJob.BuildingBlocks.Infrastructure.Events;

/// <summary>
/// Resolves all <see cref="IEventHandler{TEvent}"/> implementations from the DI
/// container and invokes them synchronously in-process. Registered as scoped so
/// handlers can use request-scoped services (e.g. EF Core DbContexts).
/// </summary>
public sealed class InProcessEventDispatcher(
    IServiceProvider serviceProvider,
    ILogger<InProcessEventDispatcher> logger) : IEventDispatcher
{
    public async Task DispatchAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        var handlers = serviceProvider.GetService<IEnumerable<IEventHandler<TEvent>>>()?.ToArray()
            ?? Array.Empty<IEventHandler<TEvent>>();

        if (handlers.Length == 0)
        {
            logger.LogWarning("No handler registered for domain event {Event}", typeof(TEvent).Name);
            return;
        }

        foreach (var handler in handlers)
        {
            await handler.HandleAsync(domainEvent, cancellationToken);
        }
    }

    /// <summary>
    /// Dispatches a domain event whose concrete type is only known at runtime,
    /// without relying on <c>dynamic</c> (DLR binding). Handlers are resolved by
    /// the event's runtime type, keeping the call site statically verifiable.
    /// </summary>
    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var eventType = domainEvent?.GetType() ?? throw new ArgumentNullException(nameof(domainEvent));
        var handlerType = typeof(IEventHandler<>).MakeGenericType(eventType);
        var handlers = (serviceProvider.GetService(typeof(IEnumerable<>).MakeGenericType(handlerType)) as IEnumerable)
            ?.Cast<object>().ToArray() ?? Array.Empty<object>();

        if (handlers.Length == 0)
        {
            logger.LogWarning("No handler registered for domain event {Event}", eventType.Name);
            return;
        }

        // Resolve the handler method by runtime type instead of 'dynamic' so the
        // dispatch path is statically verifiable at the call site (no DLR binding).
        var method = handlerType.GetMethod(nameof(IEventHandler<IDomainEvent>.HandleAsync))!;
        foreach (var handler in handlers)
        {
            await (Task)(method.Invoke(handler, new object?[] { domainEvent, cancellationToken }) ?? Task.CompletedTask);
        }
    }
}

