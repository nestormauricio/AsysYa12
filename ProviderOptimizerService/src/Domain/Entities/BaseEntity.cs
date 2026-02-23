namespace ProviderOptimizerService.Domain.Entities;

/// <summary>Base class for all domain entities. Provides identity and domain events.</summary>
public abstract class BaseEntity
{
    public Guid Id { get; protected set; }

    private readonly List<object> _domainEvents = [];
    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(object domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();
}
