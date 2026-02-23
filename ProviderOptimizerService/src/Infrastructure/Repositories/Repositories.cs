using Microsoft.EntityFrameworkCore;
using ProviderOptimizerService.Domain.Entities;
using ProviderOptimizerService.Domain.Interfaces;
using ProviderOptimizerService.Infrastructure.Data;

namespace ProviderOptimizerService.Infrastructure.Repositories;

public sealed class ProviderRepository(AppDbContext context) : IProviderRepository
{
    public async Task<Provider?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.Providers.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<Provider>> GetAllAsync(CancellationToken ct = default)
        => await context.Providers.ToListAsync(ct);

    public async Task<IReadOnlyList<Provider>> GetAvailableAsync(CancellationToken ct = default)
        => await context.Providers.Where(p => p.IsAvailable).ToListAsync(ct);

    public async Task<IReadOnlyList<Provider>> GetAvailableByTypeAsync(ProviderType type, CancellationToken ct = default)
        => await context.Providers.Where(p => p.IsAvailable && p.Type == type).ToListAsync(ct);

    public async Task AddAsync(Provider provider, CancellationToken ct = default)
        => await context.Providers.AddAsync(provider, ct);

    public Task UpdateAsync(Provider provider, CancellationToken ct = default)
    {
        context.Providers.Update(provider);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Provider provider, CancellationToken ct = default)
    {
        context.Providers.Remove(provider);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => await context.Providers.AnyAsync(p => p.Id == id, ct);
}

public sealed class UserRepository(AppDbContext context) : IUserRepository
{
    public async Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await context.Users.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), ct);

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
        => await context.Users.AnyAsync(u => u.Email == email.ToLowerInvariant(), ct);

    public async Task AddAsync(ApplicationUser user, CancellationToken ct = default)
        => await context.Users.AddAsync(user, ct);
}
