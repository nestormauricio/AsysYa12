namespace ProviderOptimizerService.Domain.ValueObjects;

/// <summary>Estimated time of arrival value object.</summary>
public sealed class EtaEstimate
{
    public double DistanceKm { get; }
    public double EstimatedMinutes { get; }
    public DateTime EstimatedArrival { get; }

    public EtaEstimate(double distanceKm, double averageSpeedKmh = 60.0)
    {
        if (distanceKm < 0) throw new ArgumentOutOfRangeException(nameof(distanceKm));
        if (averageSpeedKmh <= 0) throw new ArgumentOutOfRangeException(nameof(averageSpeedKmh));

        DistanceKm = distanceKm;
        EstimatedMinutes = distanceKm / averageSpeedKmh * 60.0;
        EstimatedArrival = DateTime.UtcNow.AddMinutes(EstimatedMinutes);
    }
}
