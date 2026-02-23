using FluentValidation;
using MediatR;
using ProviderOptimizerService.Application.Features.Providers.Queries.GetProviders;
using ProviderOptimizerService.Domain.Exceptions;
using ProviderOptimizerService.Domain.Interfaces;
using ProviderOptimizerService.Domain.ValueObjects;

namespace ProviderOptimizerService.Application.Features.Providers.Commands.UpdateProvider;

public record UpdateProviderCommand(
    Guid Id,
    bool? IsAvailable,
    double? NewRating,
    double? Latitude,
    double? Longitude) : IRequest<ProviderDto>;

public sealed class UpdateProviderCommandHandler(IProviderRepository repository, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateProviderCommand, ProviderDto>
{
    public async Task<ProviderDto> Handle(UpdateProviderCommand request, CancellationToken cancellationToken)
    {
        var provider = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new ProviderNotFoundException(request.Id);

        if (request.IsAvailable.HasValue) provider.SetAvailability(request.IsAvailable.Value);
        if (request.NewRating.HasValue) provider.UpdateRating(request.NewRating.Value);
        if (request.Latitude.HasValue && request.Longitude.HasValue)
            provider.UpdateLocation(new GeoCoordinate(request.Latitude.Value, request.Longitude.Value));

        await repository.UpdateAsync(provider, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ProviderDto
        {
            Id = provider.Id, Name = provider.Name, PhoneNumber = provider.PhoneNumber,
            Type = provider.Type.ToString(), IsAvailable = provider.IsAvailable,
            Rating = provider.Rating, ActiveAssignments = provider.ActiveAssignments,
            TotalAssignments = provider.TotalAssignments,
            Latitude = provider.Location.Latitude, Longitude = provider.Location.Longitude
        };
    }
}
