using ProviderOptimizerService.Domain.Entities;

namespace ProviderOptimizerService.Domain.Events;

public sealed class ProviderCreatedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public Provider Provider { get; }

    public ProviderCreatedEvent(Provider provider)
    {
        Provider = provider;
    }
}
