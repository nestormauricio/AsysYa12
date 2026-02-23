using MediatR;
using ProviderOptimizerService.Application.Features.Providers.Queries.GetProviders;
using ProviderOptimizerService.Domain.Exceptions;
using ProviderOptimizerService.Domain.Interfaces;

namespace ProviderOptimizerService.Application.Features.Providers.Queries.GetProviderById;

public record GetProviderByIdQuery(Guid Id) : IRequest<ProviderDto>;

public sealed class GetProviderByIdQueryHandler(IProviderRepository repository)
    : IRequestHandler<GetProviderByIdQuery, ProviderDto>
{
    public async Task<ProviderDto> Handle(GetProviderByIdQuery request, CancellationToken cancellationToken)
    {
        var p = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new ProviderNotFoundException(request.Id);

        return new ProviderDto
        {
            Id = p.Id, Name = p.Name, PhoneNumber = p.PhoneNumber,
            Type = p.Type.ToString(), IsAvailable = p.IsAvailable,
            Rating = p.Rating, ActiveAssignments = p.ActiveAssignments,
            TotalAssignments = p.TotalAssignments,
            Latitude = p.Location.Latitude, Longitude = p.Location.Longitude
        };
    }
}
