using ProviderOptimizerService.Domain.Entities;

namespace ProviderOptimizerService.Application.Features.Optimization.DTOs;

public record OptimizationResultDto(
    Provider Provider,
    double Score,
    double RatingComponent,
    double AvailabilityComponent,
    double DistanceComponent,
    double EtaComponent,
    double DistanceKm,
    double EstimatedMinutes,
    DateTime EstimatedArrival);
