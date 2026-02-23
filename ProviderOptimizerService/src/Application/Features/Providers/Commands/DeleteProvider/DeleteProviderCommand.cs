using MediatR;
using ProviderOptimizerService.Domain.Exceptions;
using ProviderOptimizerService.Domain.Interfaces;

namespace ProviderOptimizerService.Application.Features.Providers.Commands.DeleteProvider;

public record DeleteProviderCommand(Guid Id) : IRequest;

public sealed class DeleteProviderCommandHandler(IProviderRepository repository, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteProviderCommand>
{
    public async Task Handle(DeleteProviderCommand request, CancellationToken cancellationToken)
    {
        var provider = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new ProviderNotFoundException(request.Id);

        await repository.DeleteAsync(provider, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
