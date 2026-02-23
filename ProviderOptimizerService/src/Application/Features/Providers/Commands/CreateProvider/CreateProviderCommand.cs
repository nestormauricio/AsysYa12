using FluentValidation;
using MediatR;
using ProviderOptimizerService.Application.Features.Providers.Queries.GetProviders;
using ProviderOptimizerService.Domain.Entities;
using ProviderOptimizerService.Domain.Interfaces;
using ProviderOptimizerService.Domain.ValueObjects;

namespace ProviderOptimizerService.Application.Features.Providers.Commands.CreateProvider;

public record CreateProviderCommand(
    string Name,
    ProviderType Type,
    double Latitude,
    double Longitude,
    double InitialRating = 5.0,
    string? PhoneNumber = null) : IRequest<ProviderDto>;

public sealed class CreateProviderCommandValidator : AbstractValidator<CreateProviderCommand>
{
    public CreateProviderCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.InitialRating).InclusiveBetween(1, 5);
    }
}

public sealed class CreateProviderCommandHandler(IProviderRepository repository, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateProviderCommand, ProviderDto>
{
    public async Task<ProviderDto> Handle(CreateProviderCommand request, CancellationToken cancellationToken)
    {
        var location = new GeoCoordinate(request.Latitude, request.Longitude);
        var provider = Provider.Create(request.Name, request.Type, location, request.InitialRating, request.PhoneNumber);

        await repository.AddAsync(provider, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ProviderDto
        {
            Id = provider.Id, Name = provider.Name, PhoneNumber = provider.PhoneNumber,
            Type = provider.Type.ToString(), IsAvailable = provider.IsAvailable,
            Rating = provider.Rating, ActiveAssignments = 0, TotalAssignments = 0,
            Latitude = provider.Location.Latitude, Longitude = provider.Location.Longitude
        };
    }
}
