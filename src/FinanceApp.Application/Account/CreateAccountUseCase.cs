using FinanceApp.Domain.Account;
using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.ValueObjects;

namespace FinanceApp.Application.Account;

/// <summary> UseCase responsável por criar uma nova conta financeira. </summary>
public class CreateAccountUseCase(IAccountRepository accountRepository)
{
    public async Task<Domain.Account.Account> ExecuteAsync(string name, string document, decimal initialBalance = 0.01m)
    {
        // [GRASP: Protected Variations] — Sanitiza documento aqui no use case,
        // abstraindo a complexidade de máscaras da camada de apresentação.
        var sanitizedDoc = new Document(document);
        
        var existing = await accountRepository.GetByDocument(sanitizedDoc.Value);
        if (existing is not null)
            throw new AccountException($"Já existe uma conta com o documento '{sanitizedDoc.Value}'.");

        var account = new Domain.Account.Account(Guid.NewGuid(), name, sanitizedDoc, new Money(initialBalance));
        await accountRepository.Save(account);
        return account;
    }
}
