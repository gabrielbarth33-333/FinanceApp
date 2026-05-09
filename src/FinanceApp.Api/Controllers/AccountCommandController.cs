using FinanceApp.Application.Account;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.Api.Controllers;

// [SOLID: SRP] — AccountCommandController tem uma única responsabilidade: receber e
// delegar comandos de escrita relacionados a contas (apenas POST).
// Não faz queries — isso é responsabilidade de AccountQueryController.
//
// [GRASP: Controller] — Recebe o comando HTTP, delega ao UseCase e retorna a resposta.
// Não contém lógica de negócio. É apenas o ponto de entrada da camada de apresentação.
/// <summary> Responsabilidade: escrita de contas (create). </summary>
[ApiController]
[Route("api/accounts")]
public class AccountCommandController(CreateAccountUseCase createUseCase) : ControllerBase
{
    /// <summary> Cria uma nova conta financeira. </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAccountRequest request)
    {
        // [GRASP: Controller] — Delega toda a lógica ao UseCase. O controller não toma decisões.
        var account = await createUseCase.ExecuteAsync(
            request.Name,
            request.Document,
            request.InitialBalance);

        return CreatedAtAction(
            actionName: nameof(AccountQueryController.GetById),
            controllerName: "AccountQuery",
            routeValues: new { id = account.Id },
            value: account);
    }
}

public record CreateAccountRequest(
    string Name,
    string Document,
    decimal InitialBalance = 0.01m);
