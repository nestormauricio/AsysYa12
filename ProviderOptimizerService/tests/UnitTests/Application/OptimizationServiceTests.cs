using FluentAssertions;
using ProviderOptimizerService.Domain.Entities;
using ProviderOptimizerService.Domain.Interfaces;
using ProviderOptimizerService.Domain.ValueObjects;
using ProviderOptimizerService.Infrastructure.Services;
using Xunit;

namespace UnitTests.Application;

public class OptimizationServiceTests
{
    private readonly IOptimizationService _service = new WeightedScoringOptimizationService();
    private static GeoCoordinate RequestLocation => new(-12.046374, -77.042793);

    private static Provider MakeProvider(string name, double lat, double lon,
        double rating = 4.5, bool available = true)
    {
        var p = Provider.Create(name, ProviderType.Grua, new GeoCoordinate(lat, lon), rating);
        if (!available) p.SetAvailability(false);
        return p;
    }

    [Fact]
    public void RankProviders_EmptyList_ShouldReturnEmpty()
    {
        var result = _service.RankProviders([], RequestLocation);
        result.Should().BeEmpty();
    }

    [Fact]
    public void RankProviders_SingleProvider_ShouldReturnOne()
    {
        var provider = MakeProvider("P1", -12.05, -77.05);
        var result = _service.RankProviders([provider], RequestLocation);
        result.Should().HaveCount(1);
    }

    [Fact]
    public void RankProviders_CloserProviderWithSameRating_ShouldRankHigher()
    {
        var near = MakeProvider("Near", -12.047, -77.043, rating: 4.0);
        var far = MakeProvider("Far", -12.200, -77.200, rating: 4.0);

        var result = _service.RankProviders([far, near], RequestLocation);

        result[0].Provider.Name.Should().Be("Near");
    }

    [Fact]
    public void RankProviders_UnavailableProvider_ShouldRankLower()
    {
        var unavailable = MakeProvider("Unavailable", -12.047, -77.043, available: false);
        var available = MakeProvider("Available", -12.150, -77.100);

        var result = _service.RankProviders([unavailable, available], RequestLocation);

        result[0].Provider.Name.Should().Be("Available");
    }

    [Fact]
    public void RankProviders_ShouldReturnEtaEstimate()
    {
        var provider = MakeProvider("P1", -12.10, -77.10);
        var result = _service.RankProviders([provider], RequestLocation);
        result[0].Eta.EstimatedMinutes.Should().BeGreaterThan(0);
        result[0].Eta.DistanceKm.Should().BeGreaterThan(0);
    }

    [Fact]
    public void RankProviders_CustomWeights_ShouldAffectScoring()
    {
        var highRating = MakeProvider("HighRating", -12.200, -77.200, rating: 5.0);
        var lowRating = MakeProvider("LowRating", -12.047, -77.043, rating: 1.0);

        // Weight fully on rating — high rating should win despite distance
        var ratingWeights = new ScoringWeights(Rating: 1.0, Availability: 0, Distance: 0, Eta: 0);
        var result = _service.RankProviders([highRating, lowRating], RequestLocation, ratingWeights);

        result[0].Provider.Name.Should().Be("HighRating");
    }
}
