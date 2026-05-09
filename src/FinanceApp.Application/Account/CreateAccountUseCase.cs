using FinanceApp.Domain.Account;
using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.ValueObjects;

namespace FinanceApp.Application.Account;

/// <summary> UseCase responsável por criar uma nova conta financeira. </summary>
public class CreateAccountUseCase(IAccountRepository accountRepository)
{
    public async Task<Domain.Account.Account> ExecuteAsync(string name, string document, decimal initialBalance = 0.01m)
    {
        var existing = await accountRepository.GetByDocument(document);
        if (existing is not null)
            throw new AccountException($"Já existe uma conta com o documento '{document}'.");

        var account = new Domain.Account.Account(Guid.NewGuid(), name, document, new Money(initialBalance));
        await accountRepository.Save(account);
        return account;
    }
}
