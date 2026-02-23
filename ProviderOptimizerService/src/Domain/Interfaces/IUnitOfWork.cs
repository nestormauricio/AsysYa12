namespace ProviderOptimizerService.Domain.Interfaces;

/// <summary>
/// Unit of Work pattern — abstracts persistence transaction boundary.
/// Explicit transactions (#32) prevent partial writes when multiple
/// aggregates must be updated atomically (e.g. assigning a provider
/// and updating ActiveAssignments in the same commit).
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>
    /// Begins an explicit database transaction (#32).
    /// Use when multiple repository operations must succeed or fail together.
    /// </summary>
    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken ct = default);
}

/// <summary>Represents an active database transaction. Dispose to rollback.</summary>
public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}
