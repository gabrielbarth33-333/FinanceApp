using FinanceApp.Domain.Account;
using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.Transaction;

namespace FinanceApp.Application.Transaction;

/// <summary> UseCase responsável por listar transações de uma conta num período, com balance do período. </summary>
public class ListTransactionsUseCase(
    IAccountRepository accountRepository,
    ITransactionRepository transactionRepository)
{
    public async Task<TransactionPeriodResult> ExecuteAsync(
        string document,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var account = await accountRepository.GetByDocument(document)
            ?? throw new AccountException($"Conta com documento '{document}' não encontrada.");

        // Trunca para data (sem hora) e aplica padrão de 1 mês quando não informado
        var start = (startDate?.Date ?? DateTime.UtcNow.Date.AddMonths(-1));
        var end = (endDate?.Date ?? DateTime.UtcNow.Date).AddDays(1).AddTicks(-1);

        var transactions = await transactionRepository.ListByAccountIdAndPeriod(account.Id, start, end);

        var totalIncome = transactions
            .Where(t => t.Type == TransactionType.Income)
            .Sum(t => t.Amount.Value);

        var totalExpense = transactions
            .Where(t => t.Type == TransactionType.Expense)
            .Sum(t => t.Amount.Value);

        return new TransactionPeriodResult(
            StartDate: start,
            EndDate: end.Date,
            Transactions: transactions,
            TotalIncome: totalIncome,
            TotalExpense: totalExpense,
            NetBalance: totalIncome - totalExpense);
    }
}

