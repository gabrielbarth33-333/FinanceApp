using FinanceApp.Catalog.Category;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Infrastructure.Persistence;

/// <summary> <<Repository>> Implementação EF Core de ICategoryRepository. </summary>
public class CategoryRepository(FinanceAppDbContext context) : ICategoryRepository
{
    // [SOLID: SRP] — Responsável apenas por acesso a dados de Category
    // [GRASP: Information Expert] — Conhece detalhes de persistência e como buscar ignorando case/acentuação
    /// <summary> Busca categoria por nome, ignorando case e acentuação. Ex: "Alimentacao" encontra "Alimentação". </summary>
    public async Task<Category?> GetByName(string name)
    {
        // Raw SQL com FromSqlInterpolated — EF Core não consegue traduzir Unaccent() em LINQ,
        // então usamos SQL bruto com interpolação segura (previne SQL injection)
        return await context.Categories
            .FromSqlInterpolated($@"
                SELECT id, name FROM categories 
                WHERE unaccent(LOWER(name)) = unaccent(LOWER({name}))")
            .FirstOrDefaultAsync();
    }

    public async Task<List<Category>> ListAll()
        => await context.Categories.OrderBy(c => c.Name).ToListAsync();
}
