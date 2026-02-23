using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProviderOptimizerService.Application.Features.Optimization.Queries.RankProviders;
using ProviderOptimizerService.Application.Features.Providers.Queries.GetAvailableProviders;
using ProviderOptimizerService.Domain.Entities;
using ProviderOptimizerService.Domain.Interfaces;

namespace ProviderOptimizerService.API.Controllers;

[ApiController]
[Authorize]
public class OptimizationController : ControllerBase
{
    private readonly IMediator _mediator;

    public OptimizationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// POST /optimize — Ranks available providers for a given location using the
    /// weighted scoring algorithm (ETA + distance + rating + availability).
    /// Criterion #26 — exact endpoint as required by the test specification.
    /// </summary>
    [HttpPost("/optimize")]
    [ProducesResponseType(typeof(IReadOnlyList<OptimizationResultResponse>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Optimize([FromBody] OptimizeRequest request, CancellationToken ct)
    {
        var query = new RankProvidersQuery(
            request.Latitude,
            request.Longitude,
            request.RequiredType,
            request.Weights is null ? null : new ScoringWeights(
                request.Weights.Rating,
                request.Weights.Availability,
                request.Weights.Distance,
                request.Weights.Eta));

        var results = await _mediator.Send(query, ct);
        return Ok(results.Select(r => new OptimizationResultResponse(r)));
    }
}

// ─── Request / Response DTOs (no domain types exposed directly) ─────────────

public record OptimizeRequest(
    double Latitude,
    double Longitude,
    ProviderType? RequiredType,
    WeightsRequest? Weights);

public record WeightsRequest(
    double Rating = 0.30,
    double Availability = 0.25,
    double Distance = 0.25,
    double Eta = 0.20);

public record OptimizationResultResponse
{
    public Guid ProviderId { get; init; }
    public string ProviderName { get; init; }
    public string? ProviderPhone { get; init; }
    public double Score { get; init; }
    public double RatingComponent { get; init; }
    public double AvailabilityComponent { get; init; }
    public double DistanceComponent { get; init; }
    public double EtaComponent { get; init; }
    public double DistanceKm { get; init; }
    public double EstimatedMinutes { get; init; }
    public DateTime EstimatedArrival { get; init; }

    public OptimizationResultResponse(
        Application.Features.Optimization.DTOs.OptimizationResultDto r)
    {
        ProviderId = r.Provider.Id;
        ProviderName = r.Provider.Name;
        ProviderPhone = r.Provider.PhoneNumber;
        Score = r.Score;
        RatingComponent = r.RatingComponent;
        AvailabilityComponent = r.AvailabilityComponent;
        DistanceComponent = r.DistanceComponent;
        EtaComponent = r.EtaComponent;
        DistanceKm = r.DistanceKm;
        EstimatedMinutes = r.EstimatedMinutes;
        EstimatedArrival = r.EstimatedArrival;
    }
}
