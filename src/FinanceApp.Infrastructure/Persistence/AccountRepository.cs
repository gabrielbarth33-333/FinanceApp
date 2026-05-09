using FinanceApp.Domain.Account;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Infrastructure.Persistence;

/// <summary> <<Repository>> Implementação EF Core de IAccountRepository. </summary>
public class AccountRepository(FinanceAppDbContext context) : IAccountRepository
{
    public async Task<Account?> GetById(Guid accountId)
        => await context.Accounts.FirstOrDefaultAsync(a => a.Id == accountId);

    public async Task<Account?> GetByDocument(string document)
        => await context.Accounts.FirstOrDefaultAsync(a => a.Document == document);

    public async Task Save(Account account)
    {
        var exists = await context.Accounts.AnyAsync(a => a.Id == account.Id);
        if (exists)
            context.Accounts.Update(account);
        else
            await context.Accounts.AddAsync(account);

        await context.SaveChangesAsync();
    }
}
