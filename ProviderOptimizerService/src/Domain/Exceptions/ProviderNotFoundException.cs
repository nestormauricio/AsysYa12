namespace ProviderOptimizerService.Domain.Exceptions;

public class ProviderNotFoundException : DomainException
{
    public ProviderNotFoundException(Guid id)
        : base($"Provider with ID '{id}' was not found.") { }
}
