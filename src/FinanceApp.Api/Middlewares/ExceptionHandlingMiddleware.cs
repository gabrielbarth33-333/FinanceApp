using FinanceApp.Catalog.Exceptions;
using FinanceApp.Domain.Exceptions;
using System.Text.Json;

namespace FinanceApp.Api.Middlewares;

// [SOLID: SRP] — ExceptionHandlingMiddleware tem uma única responsabilidade:
// traduzir exceções de domínio em respostas HTTP padronizadas.
// Nenhum controller precisa lidar com try/catch para erros de negócio.
//
// [SOLID: OCP] — Para adicionar tratamento de um novo tipo de exceção (ex: BudgetException),
// basta adicionar um novo bloco catch. Nenhum código existente é alterado.
//
// [SOLID: LSP] — AccountException e CategoryException são tratados antes de DomainException
// porque são subtipos com semântica diferente (404 vs 400).
// Isso respeita a hierarquia: subclasses mantêm o contrato da base mas têm comportamento próprio.
/// <summary> Middleware que captura exceções de domínio e retorna respostas HTTP padronizadas. </summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        // Subtipos capturados antes da base (LSP): semântica de "não encontrado" → 404
        catch (AccountException ex)
        {
            logger.LogWarning(ex, "AccountException: {Message}", ex.Message);
            await WriteErrorAsync(context, StatusCodes.Status404NotFound, ex.Message);
        }
        catch (CategoryException ex)
        {
            logger.LogWarning(ex, "CategoryException: {Message}", ex.Message);
            await WriteErrorAsync(context, StatusCodes.Status404NotFound, ex.Message);
        }
        // Base DomainException: regra de negócio violada → 400
        catch (DomainException ex)
        {
            logger.LogWarning(ex, "DomainException: {Message}", ex.Message);
            await WriteErrorAsync(context, StatusCodes.Status400BadRequest, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro inesperado");
            await WriteErrorAsync(context, StatusCodes.Status500InternalServerError, "Ocorreu um erro inesperado.");
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, int statusCode, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var body = JsonSerializer.Serialize(new { error = message });
        await context.Response.WriteAsync(body);
    }
}
