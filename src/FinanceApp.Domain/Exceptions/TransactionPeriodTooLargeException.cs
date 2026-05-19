namespace FinanceApp.Domain.Exceptions;

/// <summary> Exceção para quando o período solicitado excede o máximo permitido (1 ano). </summary>
public class TransactionPeriodTooLargeException() : DomainException("Período máximo permitido é de 1 ano.");
