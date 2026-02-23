using ProviderOptimizerService.Domain.Entities;
using ProviderOptimizerService.Domain.Interfaces;
using ProviderOptimizerService.Domain.ValueObjects;

namespace ProviderOptimizerService.Infrastructure.Services;

/// <summary>
/// Weighted scoring optimization service.
/// Score = w_rating * normalizedRating
///       + w_availability * availabilityScore
///       + w_distance * (1 - normalizedDistance)
///       + w_eta * (1 - normalizedEta)
/// Higher is better. All components are normalized to [0,1].
/// </summary>
public sealed class WeightedScoringOptimizationService : IOptimizationService
{
    public IReadOnlyList<(Provider Provider, ProviderScore Score, EtaEstimate Eta)> RankProviders(
        IEnumerable<Provider> providers,
        GeoCoordinate requestLocation,
        ScoringWeights? weights = null)
    {
        weights ??= new ScoringWeights();
        var list = providers.ToList();
        if (list.Count == 0) return [];

        var distances = list.Select(p => p.Location.DistanceTo(requestLocation)).ToList();
        var etas = distances.Select(d => new EtaEstimate(d)).ToList();

        double minDist = distances.Min(), maxDist = distances.Max();
        double minEta = etas.Min(e => e.EstimatedMinutes), maxEta = etas.Max(e => e.EstimatedMinutes);

        var distRange = maxDist - minDist;
        var etaRange = maxEta - minEta;

        var results = list.Select((provider, i) =>
        {
            var distNorm = distRange > 0 ? (distances[i] - minDist) / distRange : 0;
            var etaNorm = etaRange > 0 ? (etas[i].EstimatedMinutes - minEta) / etaRange : 0;
            var ratingNorm = (provider.Rating - 1.0) / 4.0; // 1-5 range → 0-1
            var availScore = provider.IsAvailable
                ? 1.0 - Math.Min(provider.ActiveAssignments / 5.0, 1.0)
                : 0.0;

            var score = new ProviderScore(
                ratingComponent: weights.Rating * ratingNorm,
                availabilityComponent: weights.Availability * availScore,
                distanceComponent: weights.Distance * (1 - distNorm),
                etaComponent: weights.Eta * (1 - etaNorm));

            return (Provider: provider, Score: score, Eta: etas[i]);
        })
        .OrderByDescending(r => r.Score.Value)
        .ToList();

        return results;
    }
}
