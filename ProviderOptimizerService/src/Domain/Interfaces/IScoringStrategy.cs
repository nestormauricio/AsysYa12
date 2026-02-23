using ProviderOptimizerService.Domain.Entities;
using ProviderOptimizerService.Domain.ValueObjects;

namespace ProviderOptimizerService.Domain.Interfaces;

/// <summary>
/// Strategy pattern for provider scoring algorithms. (#28)
/// Allows swapping scoring implementations without modifying OptimizationService.
/// </summary>
public interface IScoringStrategy
{
    /// <summary>Human-readable name of this strategy, used for logging and config.</summary>
    string Name { get; }

    /// <summary>
    /// Computes a score for a single provider candidate given the request location.
    /// </summary>
    ProviderScore ComputeScore(
        Provider provider,
        double distanceKm,
        double etaMinutes,
        double maxDistanceKm,
        double minDistanceKm,
        double maxEtaMinutes,
        double minEtaMinutes,
        ScoringWeights weights);
}
