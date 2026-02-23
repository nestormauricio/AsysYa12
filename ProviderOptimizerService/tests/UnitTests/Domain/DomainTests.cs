using FluentAssertions;
using ProviderOptimizerService.Domain.Entities;
using ProviderOptimizerService.Domain.Exceptions;
using ProviderOptimizerService.Domain.ValueObjects;
using Xunit;

namespace UnitTests.Domain;

public class GeoCoordinateTests
{
    [Fact]
    public void Constructor_ValidCoordinates_ShouldCreate()
    {
        var coord = new GeoCoordinate(-12.046374, -77.042793);
        coord.Latitude.Should().Be(-12.046374);
        coord.Longitude.Should().Be(-77.042793);
    }

    [Theory]
    [InlineData(-91, 0)]
    [InlineData(91, 0)]
    public void Constructor_InvalidLatitude_ShouldThrow(double lat, double lon)
    {
        var act = () => new GeoCoordinate(lat, lon);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void DistanceTo_SamePoint_ShouldBeZero()
    {
        var coord = new GeoCoordinate(-12.046374, -77.042793);
        coord.DistanceTo(coord).Should().BeApproximately(0, 0.001);
    }

    [Fact]
    public void DistanceTo_KnownDistance_ShouldBeCorrect()
    {
        // Lima to Callao ~13km
        var lima = new GeoCoordinate(-12.046374, -77.042793);
        var callao = new GeoCoordinate(-12.049, -77.148);
        lima.DistanceTo(callao).Should().BeInRange(9, 15);
    }
}

public class ProviderTests
{
    private static GeoCoordinate DefaultLocation => new(-12.046374, -77.042793);

    [Fact]
    public void Create_ValidData_ShouldCreateProvider()
    {
        var provider = Provider.Create("Grúas Lima", ProviderType.Grua, DefaultLocation);
        provider.Name.Should().Be("Grúas Lima");
        provider.IsAvailable.Should().BeTrue();
        provider.Rating.Should().Be(5.0);
        provider.DomainEvents.Should().HaveCount(1);
    }

    [Fact]
    public void Create_EmptyName_ShouldThrow()
    {
        var act = () => Provider.Create("", ProviderType.Grua, DefaultLocation);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Create_InvalidRating_ShouldThrow(double rating)
    {
        var act = () => Provider.Create("Test", ProviderType.Grua, DefaultLocation, rating);
        act.Should().Throw<InvalidRatingException>();
    }

    [Fact]
    public void SetAvailability_ChangeState_ShouldRaiseDomainEvent()
    {
        var provider = Provider.Create("Test", ProviderType.Grua, DefaultLocation);
        provider.ClearDomainEvents();

        provider.SetAvailability(false);

        provider.IsAvailable.Should().BeFalse();
        provider.DomainEvents.Should().HaveCount(1);
    }

    [Fact]
    public void SetAvailability_SameState_ShouldNotRaiseEvent()
    {
        var provider = Provider.Create("Test", ProviderType.Grua, DefaultLocation);
        provider.ClearDomainEvents();

        provider.SetAvailability(true); // already true

        provider.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void UpdateRating_ValidRating_ShouldUpdate()
    {
        var provider = Provider.Create("Test", ProviderType.Grua, DefaultLocation);
        provider.UpdateRating(3.75);
        provider.Rating.Should().Be(3.75);
    }
}

public class ProviderScoreTests
{
    [Fact]
    public void Value_ShouldBeSumOfComponents()
    {
        var score = new ProviderScore(0.3, 0.25, 0.25, 0.20);
        score.Value.Should().BeApproximately(1.0, 0.001);
    }

    [Fact]
    public void CompareTo_HigherScore_ShouldBeGreater()
    {
        var high = new ProviderScore(0.3, 0.25, 0.25, 0.20);
        var low = new ProviderScore(0.1, 0.1, 0.1, 0.1);
        high.CompareTo(low).Should().BePositive();
    }
}
