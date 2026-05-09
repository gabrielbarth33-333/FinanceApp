using FinanceApp.Application.Account;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.Api.Controllers;

// [SOLID: SRP] — AccountQueryController tem uma única responsabilidade: receber e
// delegar consultas de leitura de contas (apenas GETs).
// Não modifica estado — isso é responsabilidade de AccountCommandController.
//
// [GRASP: Controller] — Delega ao UseCase sem conter lógica de negócio.
/// <summary> Responsabilidade: leitura de contas (get by id, get by document). </summary>
[ApiController]
[Route("api/accounts")]
public class AccountQueryController(
    GetAccountUseCase getUseCase,
    GetAccountByDocumentUseCase getByDocumentUseCase) : ControllerBase
{
    /// <summary> Retorna os dados de uma conta pelo Id. </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var account = await getUseCase.ExecuteAsync(id);
        return Ok(account);
    }

    /// <summary> Retorna os dados de uma conta pelo documento (CPF/CNPJ). </summary>
    [HttpGet("document/{document}")]
    public async Task<IActionResult> GetByDocument(string document)
    {
        var account = await getByDocumentUseCase.ExecuteAsync(document);
        return Ok(account);
    }
}
