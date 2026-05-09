using FinanceApp.Application.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace FinanceApp.Infrastructure.Persistence;

/// <summary> Implementação de IUnitOfWork usando transação EF Core. </summary>
public class UnitOfWork(FinanceAppDbContext context) : IUnitOfWork
{
    private IDbContextTransaction? _transaction;

    public async Task BeginAsync()
        => _transaction = await context.Database.BeginTransactionAsync();

    public async Task CommitAsync()
    {
        if (_transaction is null)
            return;

        await _transaction.CommitAsync();
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackAsync()
    {
        if (_transaction is null)
            return;

        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        _transaction = null;
    }
}
