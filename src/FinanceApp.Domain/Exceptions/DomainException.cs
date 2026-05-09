namespace FinanceApp.Domain.Exceptions;

/// <summary> Exceção base para erros de regra de negócio do domínio. </summary>
public class DomainException(string message) : Exception(message);
