namespace IranJob.SharedKernel.Events;

/// <summary>
/// Marker interface for domain events that are dispatched in-process
/// after an aggregate is persisted.
/// </summary>
public interface IDomainEvent;
