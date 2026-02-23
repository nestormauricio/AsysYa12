using FluentValidation;
using MediatR;
using ProviderOptimizerService.Application.Features.Auth.DTOs;
using ProviderOptimizerService.Domain.Entities;
using ProviderOptimizerService.Domain.Interfaces;

namespace ProviderOptimizerService.Application.Features.Auth.Commands.Register;

public record RegisterCommand(string Username, string Email, string Password) : IRequest<AuthResultDto>;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MinimumLength(3).MaximumLength(50);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}

public sealed class RegisterCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RegisterCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (await userRepository.ExistsByEmailAsync(request.Email, cancellationToken))
            throw new InvalidOperationException("Email is already registered.");

        var hash = passwordHasher.Hash(request.Password);
        var user = ApplicationUser.Create(request.Username, request.Email, hash);
        await userRepository.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var (token, expiresAt) = jwtTokenService.GenerateToken(user);
        return new AuthResultDto(token, expiresAt, user.Username, user.Email, user.Role.ToString());
    }
}
