namespace FinanceApp.Catalog.Exceptions;

/// <summary> Exceção para erros relacionados a categorias (não encontrada, nome inválido, etc.). </summary>
public class CategoryException(string message) : Exception(message);
