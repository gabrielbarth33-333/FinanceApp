using FinanceApp.Domain.Exceptions;

namespace FinanceApp.Domain.ValueObjects;

// [SOLID: SRP] — CategoryRef tem uma única responsabilidade: representar a referência interna
// a uma categoria dentro do Financial Context. Não sabe nada sobre o Catalog Context.
//
// [GRASP: Low Coupling] — O Financial Context nunca importa Category diretamente.
// CategoryRef é a tradução local — mantém o acoplamento entre contextos em zero.
/// <summary> <<ValueObject>> Referência interna a uma categoria dentro do Financial Context. </summary>
public record CategoryRef
{
    public Guid CategoryId { get; }
    public string Name { get; }

    public CategoryRef(Guid categoryId, string name)
    {
        if (categoryId == Guid.Empty)
            throw new DomainException("CategoryId inválido.");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Nome da categoria é obrigatório.");

        CategoryId = categoryId;
        Name = name;
    }
}
