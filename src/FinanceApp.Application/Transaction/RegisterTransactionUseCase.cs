using FinanceApp.Application.Interfaces;
using FinanceApp.Domain.ACL;
using FinanceApp.Domain.Account;
using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.Transaction;
using FinanceApp.Domain.ValueObjects;

namespace FinanceApp.Application.Transaction;

// [SOLID: SRP] — RegisterTransactionUseCase tem uma única responsabilidade: orquestrar
// o fluxo de registro de uma transação. Não valida saldo (TransactionDomainService),
// não traduz categorias (CategoryACL), não persiste diretamente (repositórios).
//
// [GRASP: Controller] — Atua como "controller" de caso de uso: recebe o comando da camada
// de apresentação, coordena os objetos de domínio e infraestrutura, e retorna o resultado.
// Não contém lógica de negócio — apenas orquestração.
//
// [SOLID: DIP] — Todas as dependências são interfaces: IAccountRepository, ITransactionRepository,
// ICategoryACL, ITransactionDomainService, IUnitOfWork.
// O UseCase não conhece EF Core, PostgreSQL, nem nenhuma implementação concreta.
/// <summary> UseCase responsável por registrar uma nova transação financeira. </summary>
public class RegisterTransactionUseCase(
    IAccountRepository accountRepository,       // [DIP] abstração do repositório
    ITransactionRepository transactionRepository, // [DIP] abstração do repositório
    ICategoryACL categoryACL,                   // [DIP + Indirection] abstração da ACL
    ITransactionDomainService domainService,    // [DIP] abstração do serviço de domínio
    IUnitOfWork unitOfWork)                     // [DIP] abstração da transação de banco
{
    public async Task ExecuteAsync(
        string document,
        decimal amount,
        TransactionType type,
        string categoryName,
        DateTime? date = null)
    {
        // Resolve documento → conta (usuário nunca lida com Guid)
        var account = await accountRepository.GetByDocument(document)
            ?? throw new AccountException($"Conta com documento '{document}' não encontrada.");

        var money = new Money(amount);
        var transactionDate = date ?? DateTime.UtcNow;

        // [GRASP: Expert] — Delega a validação de saldo ao DomainService,
        // que é o especialista por coordenar Account + regra de negócio.
        if (type == TransactionType.Expense)
            await domainService.ValidateBalance(account.Id, money);

        // [GRASP: Indirection] — GetCategory passa pela ICategoryACL (barreira anti-corrupção).
        // O UseCase nunca toca em Category — recebe apenas CategoryRef (tipo local).
        var categoryRef = await categoryACL.GetCategory(categoryName);

        // [GRASP: Creator] — RegisterTransactionUseCase cria Transaction porque possui
        // todos os dados necessários para instanciá-la corretamente.
        var transaction = new Domain.Transaction.Transaction(account.Id, transactionDate, categoryRef, type, money);

        // [SOLID: DIP + SRP] — IUnitOfWork isola o mecanismo de transação de banco.
        // O UseCase não sabe se usa EF Core, Dapper ou outra coisa — apenas define o escopo.
        await unitOfWork.BeginAsync();
        try
        {
            await transactionRepository.Add(transaction);

            // [GRASP: Expert] — UpdateBalance é responsabilidade de Account (ver Account.cs).
            account.UpdateBalance(money, type);
            await accountRepository.Save(account);

            await unitOfWork.CommitAsync();
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
    }
}
