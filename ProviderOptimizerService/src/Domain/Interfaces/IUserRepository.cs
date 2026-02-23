using ProviderOptimizerService.Domain.Entities;

namespace ProviderOptimizerService.Domain.Interfaces;

public interface IUserRepository
{
    Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
    Task AddAsync(ApplicationUser user, CancellationToken ct = default);
}
