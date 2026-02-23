using ProviderOptimizerService.Domain.Events;
using ProviderOptimizerService.Domain.Exceptions;
using ProviderOptimizerService.Domain.ValueObjects;

namespace ProviderOptimizerService.Domain.Entities;

public enum ProviderType { Grua = 1, Cerrajeria = 2, Bateria = 3, Neumatico = 4 }

/// <summary>Provider aggregate root. Encapsulates business rules around availability and rating.</summary>
public class Provider : BaseEntity
{
    public string Name { get; private set; } = default!;
    public string? PhoneNumber { get; private set; }
    public ProviderType Type { get; private set; }
    public bool IsAvailable { get; private set; }
    public double Rating { get; private set; }
    public int ActiveAssignments { get; private set; }
    public int TotalAssignments { get; private set; }
    public GeoCoordinate Location { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Optimistic concurrency token — EF Core throws DbUpdateConcurrencyException
    /// if two concurrent threads attempt to assign the same provider simultaneously (#33).
    /// The caller must retry with the next-best candidate.
    /// </summary>
    public byte[]? RowVersion { get; private set; }

    private Provider() { }

    public static Provider Create(
        string name,
        ProviderType type,
        GeoCoordinate location,
        double initialRating = 5.0,
        string? phoneNumber = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Provider name is required.", nameof(name));
        if (initialRating < 1 || initialRating > 5)
            throw new InvalidRatingException(initialRating);

        var provider = new Provider
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = type,
            Location = location ?? throw new ArgumentNullException(nameof(location)),
            Rating = initialRating,
            PhoneNumber = phoneNumber,
            IsAvailable = true,
            ActiveAssignments = 0,
            TotalAssignments = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        provider.AddDomainEvent(new ProviderCreatedEvent(provider.Id, provider.Name, provider.Type));
        return provider;
    }

    public void SetAvailability(bool available)
    {
        if (IsAvailable == available) return;
        IsAvailable = available;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new ProviderAvailabilityChangedEvent(Id, available));
    }

    public void UpdateRating(double newRating)
    {
        if (newRating < 1 || newRating > 5) throw new InvalidRatingException(newRating);
        Rating = Math.Round(newRating, 2);
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateLocation(GeoCoordinate location)
    {
        Location = location ?? throw new ArgumentNullException(nameof(location));
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementActiveAssignments()
    {
        ActiveAssignments++;
        TotalAssignments++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void DecrementActiveAssignments()
    {
        if (ActiveAssignments > 0) ActiveAssignments--;
        UpdatedAt = DateTime.UtcNow;
    }
}
