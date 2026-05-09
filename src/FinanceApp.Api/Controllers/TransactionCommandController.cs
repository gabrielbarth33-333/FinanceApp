using FinanceApp.Application.Transaction;
using FinanceApp.Domain.Transaction;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.Api.Controllers;

// [SOLID: SRP] — TransactionCommandController tem uma única responsabilidade:
// receber comandos de escrita de transações (apenas POST).
//
// [GRASP: Controller] — Ponto de entrada HTTP para o caso de uso de registro.
// Não valida regras, não calcula saldo — apenas delega ao UseCase.
/// <summary> Responsabilidade: escrita de transações (register). </summary>
[ApiController]
[Route("api/transactions")]
public class TransactionCommandController(RegisterTransactionUseCase registerUseCase) : ControllerBase
{
    /// <summary> Registra uma nova transação (receita ou despesa). </summary>
    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterTransactionRequest request)
    {
        await registerUseCase.ExecuteAsync(
            request.Document,
            request.Amount,
            request.Type,
            request.CategoryName,
            request.Date);

        return Created();
    }
}

public record RegisterTransactionRequest(
    string Document,
    decimal Amount,
    TransactionType Type,
    string CategoryName,
    DateTime? Date);
