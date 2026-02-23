using Microsoft.EntityFrameworkCore.Storage;
using ProviderOptimizerService.Domain.Interfaces;
using ProviderOptimizerService.Infrastructure.Data;

namespace ProviderOptimizerService.Infrastructure.Repositories;

public sealed class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => context.SaveChangesAsync(ct);

    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken ct = default)
    {
        var transaction = await context.Database.BeginTransactionAsync(ct);
        return new UnitOfWorkTransaction(transaction);
    }
}

internal sealed class UnitOfWorkTransaction(IDbContextTransaction transaction) : IUnitOfWorkTransaction
{
    public Task CommitAsync(CancellationToken ct = default) => transaction.CommitAsync(ct);
    public Task RollbackAsync(CancellationToken ct = default) => transaction.RollbackAsync(ct);
    public async ValueTask DisposeAsync() => await transaction.DisposeAsync();
}
