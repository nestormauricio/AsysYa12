using MediatR;
using ProviderOptimizerService.Application.Features.Optimization.DTOs;
using ProviderOptimizerService.Domain.Entities;
using ProviderOptimizerService.Domain.Interfaces;
using ProviderOptimizerService.Domain.ValueObjects;

namespace ProviderOptimizerService.Application.Features.Optimization.Queries.RankProviders;

public record RankProvidersQuery(
    double Latitude,
    double Longitude,
    ProviderType? RequiredType,
    ScoringWeights? Weights) : IRequest<IReadOnlyList<OptimizationResultDto>>;

public sealed class RankProvidersQueryHandler(
    IProviderRepository repository,
    IOptimizationService optimizationService,
    ICacheService cacheService)
    : IRequestHandler<RankProvidersQuery, IReadOnlyList<OptimizationResultDto>>
{
    public async Task<IReadOnlyList<OptimizationResultDto>> Handle(RankProvidersQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"optimize:{request.Latitude:F4}:{request.Longitude:F4}:{request.RequiredType}";
        var cached = await cacheService.GetAsync<IReadOnlyList<OptimizationResultDto>>(cacheKey, cancellationToken);
        if (cached is not null) return cached;

        var providers = request.RequiredType.HasValue
            ? await repository.GetAvailableByTypeAsync(request.RequiredType.Value, cancellationToken)
            : await repository.GetAvailableAsync(cancellationToken);

        var requestLocation = new GeoCoordinate(request.Latitude, request.Longitude);
        var ranked = optimizationService.RankProviders(providers, requestLocation, request.Weights);

        var results = ranked.Select(r => new OptimizationResultDto(
            r.Provider,
            r.Score.Value,
            r.Score.RatingComponent,
            r.Score.AvailabilityComponent,
            r.Score.DistanceComponent,
            r.Score.EtaComponent,
            r.Eta.DistanceKm,
            r.Eta.EstimatedMinutes,
            r.Eta.EstimatedArrival
        )).ToList();

        await cacheService.SetAsync(cacheKey, results, TimeSpan.FromSeconds(30), cancellationToken);
        return results;
    }
}
