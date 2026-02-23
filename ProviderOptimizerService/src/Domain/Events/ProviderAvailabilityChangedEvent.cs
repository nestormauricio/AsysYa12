namespace ProviderOptimizerService.Domain.Events;

public sealed class ProviderAvailabilityChangedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public Guid ProviderId { get; }
    public bool IsAvailable { get; }

    public ProviderAvailabilityChangedEvent(Guid providerId, bool isAvailable)
    {
        ProviderId = providerId;
        IsAvailable = isAvailable;
    }
}
