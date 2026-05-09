using FinanceApp.Catalog.Category;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.Api.Controllers;

/// <summary> Controller de categorias — somente leitura (GET). </summary>
[ApiController]
[Route("api/categories")]
public class CategoryController(ICategoryRepository categoryRepository) : ControllerBase
{
    /// <summary> Lista todas as categorias disponíveis. </summary>
    [HttpGet]
    public async Task<IActionResult> ListAll()
    {
        var categories = await categoryRepository.ListAll();
        return Ok(categories);
    }
}
