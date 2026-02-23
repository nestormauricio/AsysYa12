using MediatR;
using ProviderOptimizerService.Application.Features.Providers.Queries.GetProviders;
using ProviderOptimizerService.Domain.Entities;
using ProviderOptimizerService.Domain.Interfaces;

namespace ProviderOptimizerService.Application.Features.Providers.Queries.GetAvailableProviders;

public record GetAvailableProvidersQuery(ProviderType? Type) : IRequest<IReadOnlyList<ProviderDto>>;

public sealed class GetAvailableProvidersQueryHandler(IProviderRepository repository)
    : IRequestHandler<GetAvailableProvidersQuery, IReadOnlyList<ProviderDto>>
{
    public async Task<IReadOnlyList<ProviderDto>> Handle(GetAvailableProvidersQuery request, CancellationToken cancellationToken)
    {
        var providers = request.Type.HasValue
            ? await repository.GetAvailableByTypeAsync(request.Type.Value, cancellationToken)
            : await repository.GetAvailableAsync(cancellationToken);

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
