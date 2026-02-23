using ProviderOptimizerService.Domain.Entities;

namespace ProviderOptimizerService.Domain.Interfaces;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAt) GenerateToken(ApplicationUser user);
}
