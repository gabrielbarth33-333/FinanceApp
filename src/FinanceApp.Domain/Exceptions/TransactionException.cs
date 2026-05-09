namespace FinanceApp.Domain.Exceptions;

/// <summary> Exceção para erros relacionados a transações (valor inválido, data futura, etc.). </summary>
public class TransactionException(string message) : DomainException(message);
