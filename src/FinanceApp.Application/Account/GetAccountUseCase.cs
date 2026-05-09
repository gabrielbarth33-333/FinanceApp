using FinanceApp.Domain.Account;
using FinanceApp.Domain.Exceptions;

namespace FinanceApp.Application.Account;

/// <summary> UseCase responsável por recuperar uma conta pelo seu Id. </summary>
public class GetAccountUseCase(IAccountRepository accountRepository)
{
    public async Task<Domain.Account.Account> ExecuteAsync(Guid accountId)
    {
        return await accountRepository.GetById(accountId)
            ?? throw new AccountException($"Conta '{accountId}' não encontrada.");
    }
}
