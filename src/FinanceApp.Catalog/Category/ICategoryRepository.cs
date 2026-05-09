namespace FinanceApp.Catalog.Category;

/// <summary> Contrato de persistência para o Aggregate Root Category. </summary>
public interface ICategoryRepository
{
    Task<Category?> GetByName(string name);
    Task<List<Category>> ListAll();
}
