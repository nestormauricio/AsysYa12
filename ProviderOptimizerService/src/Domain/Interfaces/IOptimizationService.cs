using ProviderOptimizerService.Domain.Entities;
using ProviderOptimizerService.Domain.ValueObjects;

namespace ProviderOptimizerService.Domain.Interfaces;

public interface IOptimizationService
{
    IReadOnlyList<(Provider Provider, ProviderScore Score, EtaEstimate Eta)> RankProviders(
        IEnumerable<Provider> providers,
        GeoCoordinate requestLocation,
        ScoringWeights? weights = null);
}

public record ScoringWeights(
    double Rating = 0.30,
    double Availability = 0.25,
    double Distance = 0.25,
    double Eta = 0.20);
