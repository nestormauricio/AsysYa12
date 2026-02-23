using ProviderOptimizerService.Domain.Entities;

namespace ProviderOptimizerService.Domain.Interfaces;

public interface IProviderRepository
{
    Task<Provider?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Provider>> GetAllAsync(CancellationToken ct = default);
    /// <summary>Returns all available providers regardless of type.</summary>
    Task<IReadOnlyList<Provider>> GetAvailableAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Provider>> GetAvailableByTypeAsync(ProviderType type, CancellationToken ct = default);
    Task AddAsync(Provider provider, CancellationToken ct = default);
    Task UpdateAsync(Provider provider, CancellationToken ct = default);
    Task DeleteAsync(Provider provider, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
}
