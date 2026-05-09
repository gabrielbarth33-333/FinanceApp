using FinanceApp.Domain.Account;
using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.Transaction;
using FinanceApp.Domain.ValueObjects;

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
        // [GRASP: Protected Variations] — Sanitiza documento aqui no use case
        var sanitizedDoc = new Document(document);
        
        var account = await accountRepository.GetByDocument(sanitizedDoc.Value)
            ?? throw new AccountException($"Conta com documento '{sanitizedDoc.Value}' não encontrada.");

        // Trunca para data (sem hora) e aplica padrão de 1 mês quando não informado
        // DateTime.SpecifyKind garante Kind=Utc exigido pelo Npgsql em colunas timestamptz
        var start = DateTime.SpecifyKind(startDate?.Date ?? DateTime.UtcNow.Date.AddMonths(-1), DateTimeKind.Utc);
        var end = DateTime.SpecifyKind((endDate?.Date ?? DateTime.UtcNow.Date).AddDays(1).AddTicks(-1), DateTimeKind.Utc);

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

