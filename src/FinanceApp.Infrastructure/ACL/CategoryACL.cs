using FinanceApp.Catalog.Category;
using FinanceApp.Catalog.Exceptions;
using FinanceApp.Domain.ACL;
using FinanceApp.Domain.ValueObjects;

namespace FinanceApp.Infrastructure.ACL;

// [GRASP: Pure Fabrication] — CategoryACL não representa nenhum conceito do domínio financeiro.
// É uma classe artificial criada exclusivamente para isolar a integração entre contextos.
// Objetivo: alcançar baixo acoplamento sem atribuir responsabilidade espúria a objetos de domínio.
//
// [GRASP: Low Coupling] — O Financial Context nunca importa Category diretamente.
// CategoryACL é o único ponto de contato; qualquer mudança no Catalog Context
// se resolve aqui, sem propagar para o domínio financeiro.
//
// [GRASP: Indirection] — Atua como mediador entre os dois bounded contexts.
// Financial → ICategoryACL (Domain) → CategoryACL (Infrastructure) → ICategoryRepository (Catalog)
//
// [SOLID: DIP] — Reside em Infrastructure (não em Domain) porque precisa referenciar
// ICategoryRepository do Catalog. Domain declara apenas a interface ICategoryACL.
/// <summary> <<PureFabrication>> Implementação da ACL — isola o Financial Context do Catalog Context. Reside em Infrastructure pois depende de ICategoryRepository do Catalog. </summary>
public class CategoryACL(ICategoryRepository categoryRepository) : ICategoryACL
{
    public async Task<CategoryRef> GetCategory(string name)
    {
        var category = await categoryRepository.GetByName(name)
            ?? throw new CategoryException($"Categoria '{name}' não encontrada.");

        // Tradução: Category (Catalog) → CategoryRef (Financial) — barreira da ACL.
        return new CategoryRef(category.Id, category.Name);
    }
}
