namespace FinanceApp.Domain.Exceptions;

/// <summary> Exceção para quando a data de início é posterior à data de fim em consultas de transações. </summary>
public class InvalidTransactionPeriodException(string message) : DomainException(message);
