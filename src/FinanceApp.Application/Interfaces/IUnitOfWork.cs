namespace FinanceApp.Application.Interfaces;

/// <summary> Abstrai a transação de banco de dados — garante consistência em operações multi-step. </summary>
public interface IUnitOfWork
{
    Task BeginAsync();
    Task CommitAsync();
    Task RollbackAsync();
}
