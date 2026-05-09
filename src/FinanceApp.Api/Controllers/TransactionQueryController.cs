using FinanceApp.Application.Transaction;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.Api.Controllers;

// [SOLID: SRP] — TransactionQueryController tem uma única responsabilidade:
// receber consultas de leitura de transações (apenas GET).
//
// [GRASP: Controller] — Delega ao UseCase sem conter regras de negócio.
// A lógica de limite de período e cálculo de balance está no ListTransactionsUseCase.
/// <summary> Responsabilidade: leitura de transações (list by document). </summary>
[ApiController]
[Route("api/transactions")]
public class TransactionQueryController(ListTransactionsUseCase listUseCase) : ControllerBase
{
    /// <summary> Lista as transações de uma conta pelo documento (CPF/CNPJ), com filtragem opcional por período. Quando não informado, aplica o último mês. Retorna o balance do período. </summary>
    [HttpGet("document/{document}")]
    public async Task<IActionResult> ListByDocument(
        string document,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var result = await listUseCase.ExecuteAsync(document, startDate, endDate);
        return Ok(result);
    }
}
