using FinanceApp.Catalog.Category;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Infrastructure.Persistence;

/// <summary> <<Repository>> Implementação EF Core de ICategoryRepository. </summary>
public class CategoryRepository(FinanceAppDbContext context) : ICategoryRepository
{
    public async Task<Category?> GetByName(string name)
        => await context.Categories.FirstOrDefaultAsync(c => c.Name == name);

    public async Task<List<Category>> ListAll()
        => await context.Categories.OrderBy(c => c.Name).ToListAsync();
}
