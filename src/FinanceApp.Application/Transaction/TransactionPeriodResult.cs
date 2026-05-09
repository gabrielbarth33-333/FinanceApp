using TransactionEntity = FinanceApp.Domain.Transaction.Transaction;

namespace FinanceApp.Application.Transaction;

/// <summary> Resultado da consulta de transações de um período, incluindo resumo de saldo. </summary>
public record TransactionPeriodResult(
    DateTime StartDate,
    DateTime EndDate,
    List<TransactionEntity> Transactions,
    decimal TotalIncome,
    decimal TotalExpense,
    decimal NetBalance);
