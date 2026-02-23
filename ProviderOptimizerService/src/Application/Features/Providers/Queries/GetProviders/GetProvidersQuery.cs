using AutoMapper;
using MediatR;
using ProviderOptimizerService.Domain.Entities;
using ProviderOptimizerService.Domain.Interfaces;

namespace ProviderOptimizerService.Application.Features.Providers.Queries.GetProviders;

public record ProviderDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string? PhoneNumber { get; init; }
    public string Type { get; init; } = default!;
    public bool IsAvailable { get; init; }
    public double Rating { get; init; }
    public int ActiveAssignments { get; init; }
    public int TotalAssignments { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
}

public record GetProvidersQuery : IRequest<IReadOnlyList<ProviderDto>>;

public sealed class GetProvidersQueryHandler(IProviderRepository repository, IMapper mapper)
    : IRequestHandler<GetProvidersQuery, IReadOnlyList<ProviderDto>>
{
    public async Task<IReadOnlyList<ProviderDto>> Handle(GetProvidersQuery request, CancellationToken cancellationToken)
    {
        var providers = await repository.GetAllAsync(cancellationToken);
        return providers.Select(p => new ProviderDto
        {
            Id = p.Id, Name = p.Name, PhoneNumber = p.PhoneNumber,
            Type = p.Type.ToString(), IsAvailable = p.IsAvailable,
            Rating = p.Rating, ActiveAssignments = p.ActiveAssignments,
            TotalAssignments = p.TotalAssignments,
            Latitude = p.Location.Latitude, Longitude = p.Location.Longitude
        }).ToList();
    }
}
