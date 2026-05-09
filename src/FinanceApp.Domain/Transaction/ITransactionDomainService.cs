using FinanceApp.Domain.ValueObjects;

namespace FinanceApp.Domain.Transaction;

// [SOLID: DIP] — RegisterTransactionUseCase depende desta interface, não do TransactionDomainService concreto.
// Objetivo: permite substituir (ou mockar em testes) a implementação sem alterar o UseCase.
//
// [SOLID: ISP] — Interface mínima com um único método. O consumidor não é obrigado
// a depender de nada além do que efetivamente usa.
/// <summary> Contrato do serviço de domínio de transações. </summary>
public interface ITransactionDomainService
{
    Task ValidateBalance(Guid accountId, Money amount);
}
