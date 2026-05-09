using FinanceApp.Domain.Account;
using FinanceApp.Domain.Exceptions;

namespace FinanceApp.Application.Account;

/// <summary> UseCase responsável por recuperar uma conta pelo documento (CPF/CNPJ). </summary>
public class GetAccountByDocumentUseCase(IAccountRepository accountRepository)
{
    public async Task<Domain.Account.Account> ExecuteAsync(string document)
    {
        return await accountRepository.GetByDocument(document)
            ?? throw new AccountException($"Conta com documento '{document}' não encontrada.");
    }
}
