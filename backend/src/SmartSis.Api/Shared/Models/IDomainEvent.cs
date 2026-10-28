namespace SmartSis.Api.Shared.Models;

/// <summary>Marker for in-process domain events (e.g. StudentAutoDropped). Dispatching is added when the first event is needed.</summary>
public interface IDomainEvent
{
    DateTime OccurredAt { get; }
}
