using FinanceApp.Domain.Account;
using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.ValueObjects;

namespace FinanceApp.Domain.Transaction;

// [SOLID: SRP] — TransactionDomainService tem uma única responsabilidade: validar regras
// de domínio que cruzam múltiplos agregados (Account + Transaction).
// A lógica de "tem saldo?" não pertence ao UseCase (que orquestra) nem ao Account isoladamente
// — pertence ao serviço que coordena os dois.
//
// [GRASP: Expert] — ValidateBalance está aqui porque este serviço conhece tanto a regra
// de negócio (HasBalance) quanto como obter a conta (IAccountRepository).
/// <summary> Serviço de domínio responsável por validar regras que envolvem múltiplos agregados. </summary>
public class TransactionDomainService(IAccountRepository accountRepository) : ITransactionDomainService
{
    /// <summary> Valida se a conta existe e possui saldo suficiente para o valor informado. </summary>
    public async Task ValidateBalance(Guid accountId, Money amount)
    {
        // [SOLID: DIP] — Acessa IAccountRepository (interface), não AccountRepository (EF Core).
        var account = await accountRepository.GetById(accountId)
            ?? throw new AccountException($"Conta '{accountId}' não encontrada.");

        // [GRASP: Expert] — Delega a verificação de saldo para Account.HasBalance(),
        // que é quem possui a informação para respondê-la corretamente.
        if (!account.HasBalance(amount))
            throw new TransactionException("Saldo insuficiente para realizar a transação.");
    }
}
