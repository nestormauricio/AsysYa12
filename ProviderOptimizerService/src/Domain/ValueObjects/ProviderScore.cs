namespace ProviderOptimizerService.Domain.ValueObjects;

/// <summary>Represents a computed optimization score for a provider.</summary>
public sealed class ProviderScore : IEquatable<ProviderScore>, IComparable<ProviderScore>
{
    public double Value { get; }
    public double RatingComponent { get; }
    public double AvailabilityComponent { get; }
    public double DistanceComponent { get; }
    public double EtaComponent { get; }

    public ProviderScore(
        double ratingComponent,
        double availabilityComponent,
        double distanceComponent,
        double etaComponent)
    {
        RatingComponent = ratingComponent;
        AvailabilityComponent = availabilityComponent;
        DistanceComponent = distanceComponent;
        EtaComponent = etaComponent;
        Value = ratingComponent + availabilityComponent + distanceComponent + etaComponent;
    }

    public bool Equals(ProviderScore? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => Equals(obj as ProviderScore);
    public override int GetHashCode() => Value.GetHashCode();
    public int CompareTo(ProviderScore? other) => other is null ? 1 : Value.CompareTo(other.Value);
}
