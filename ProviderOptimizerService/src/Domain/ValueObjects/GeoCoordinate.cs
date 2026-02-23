namespace ProviderOptimizerService.Domain.ValueObjects;

/// <summary>Immutable geographic coordinate value object.</summary>
public sealed class GeoCoordinate : IEquatable<GeoCoordinate>
{
    public double Latitude { get; }
    public double Longitude { get; }

    public GeoCoordinate(double latitude, double longitude)
    {
        if (latitude < -90 || latitude > 90)
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90.");
        if (longitude < -180 || longitude > 180)
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180.");

        Latitude = latitude;
        Longitude = longitude;
    }

    /// <summary>Calculates distance in kilometers using the Haversine formula.</summary>
    public double DistanceTo(GeoCoordinate other)
    {
        const double EarthRadiusKm = 6371.0;
        var dLat = ToRad(other.Latitude - Latitude);
        var dLon = ToRad(other.Longitude - Longitude);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(ToRad(Latitude)) * Math.Cos(ToRad(other.Latitude))
              * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return EarthRadiusKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double ToRad(double degrees) => degrees * Math.PI / 180.0;

    public bool Equals(GeoCoordinate? other) =>
        other is not null && Latitude == other.Latitude && Longitude == other.Longitude;

    public override bool Equals(object? obj) => Equals(obj as GeoCoordinate);
    public override int GetHashCode() => HashCode.Combine(Latitude, Longitude);
    public override string ToString() => $"({Latitude}, {Longitude})";
}
