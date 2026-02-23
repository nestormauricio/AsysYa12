using MediatR;
using ProviderOptimizerService.Application.Features.Auth.DTOs;
using ProviderOptimizerService.Domain.Interfaces;

namespace ProviderOptimizerService.Application.Features.Auth.Commands.Login;

public record LoginCommand(string Email, string Password) : IRequest<AuthResultDto>;

public sealed class LoginCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService)
    : IRequestHandler<LoginCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        var (token, expiresAt) = jwtTokenService.GenerateToken(user);
        return new AuthResultDto(token, expiresAt, user.Username, user.Email, user.Role.ToString());
    }
}
