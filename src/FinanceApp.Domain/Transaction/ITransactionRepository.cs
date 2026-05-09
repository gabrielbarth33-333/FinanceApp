namespace FinanceApp.Domain.Transaction;

// [SOLID: ISP] — Interface específica para Transaction, separada de IAccountRepository.
// Cada UseCase injeta apenas o repositório que precisa — sem métodos desnecessários.
/// <summary> Contrato de persistência para o Aggregate Root Transaction. </summary>
public interface ITransactionRepository
{
    Task Add(Transaction transaction);
    Task<List<Transaction>> ListByAccountId(Guid accountId);
    Task<List<Transaction>> ListByAccountIdAndPeriod(Guid accountId, DateTime startDate, DateTime endDate);
}
