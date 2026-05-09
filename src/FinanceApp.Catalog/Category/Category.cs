namespace FinanceApp.Catalog.Category;

/// <summary> <<AggregateRoot>> Classificação das transações no Catalog Context. </summary>
public class Category
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }

    public Category(Guid id, string name)
    {
        if (id == Guid.Empty)
            throw new Exceptions.CategoryException("Id da categoria inválido.");

        if (string.IsNullOrWhiteSpace(name))
            throw new Exceptions.CategoryException("Nome da categoria é obrigatório.");

        Id = id;
        Name = name;
    }

    // Construtor sem parâmetros para o EF Core
    private Category() { Id = Guid.Empty; Name = string.Empty; }
}
