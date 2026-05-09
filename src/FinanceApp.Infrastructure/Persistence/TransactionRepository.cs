using FinanceApp.Domain.Transaction;
using Microsoft.EntityFrameworkCore;

using TransactionEntity = FinanceApp.Domain.Transaction.Transaction;

namespace FinanceApp.Infrastructure.Persistence;

/// <summary> <<Repository>> Implementação EF Core de ITransactionRepository. </summary>
public class TransactionRepository(FinanceAppDbContext context) : ITransactionRepository
{
    public async Task Add(TransactionEntity transaction)
    {
        await context.Transactions.AddAsync(transaction);
        await context.SaveChangesAsync();
    }

    public async Task<List<TransactionEntity>> ListByAccountId(Guid accountId)
        => await context.Transactions
            .Where(t => t.AccountId == accountId)
            .OrderByDescending(t => t.Date)
            .ToListAsync();

    public async Task<List<TransactionEntity>> ListByAccountIdAndPeriod(Guid accountId, DateTime startDate, DateTime endDate)
        => await context.Transactions
            .Where(t => t.AccountId == accountId && t.Date >= startDate && t.Date <= endDate)
            .OrderByDescending(t => t.Date)
            .ToListAsync();
}
