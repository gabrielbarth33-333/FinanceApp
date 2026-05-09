using FinanceApp.Domain.Account;
using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.ValueObjects;

namespace FinanceApp.Application.Account;

/// <summary> UseCase responsável por recuperar uma conta pelo documento (CPF/CNPJ). </summary>
public class GetAccountByDocumentUseCase(IAccountRepository accountRepository)
{
    public async Task<Domain.Account.Account> ExecuteAsync(string document)
    {
        // [GRASP: Protected Variations] — Sanitiza documento aqui no use case
        var sanitizedDoc = new Document(document);
        
        return await accountRepository.GetByDocument(sanitizedDoc.Value)
            ?? throw new AccountException($"Conta com documento '{sanitizedDoc.Value}' não encontrada.");
    }
}
