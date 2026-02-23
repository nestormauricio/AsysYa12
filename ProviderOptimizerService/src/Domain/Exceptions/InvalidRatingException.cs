namespace ProviderOptimizerService.Domain.Exceptions;

public class InvalidRatingException : DomainException
{
    public InvalidRatingException(decimal rating)
        : base($"Rating value '{rating}' is invalid. Must be between 0 and 5.") { }
}
