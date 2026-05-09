namespace FinanceApp.Domain.Exceptions;

/// <summary> Exceção para erros relacionados à conta (documento duplicado, conta não encontrada, etc.). </summary>
public class AccountException(string message) : DomainException(message);
