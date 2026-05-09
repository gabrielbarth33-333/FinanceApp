namespace FinanceApp.Domain.Account;

// [SOLID: DIP] — UseCase e DomainService dependem desta interface, não de AccountRepository (EF Core).
// Objetivo: módulos de alto nível (Application/Domain) não devem depender de módulos de baixo nível (Infrastructure).
//
// [SOLID: ISP] — Interface específica para Account; não mistura operações de Transaction ou Category.
// Objetivo: nenhum cliente é forçado a depender de métodos que não usa.
/// <summary> Contrato de persistência para o Aggregate Root Account. </summary>
public interface IAccountRepository
{
    Task<Account?> GetById(Guid accountId);
    Task<Account?> GetByDocument(string document);
    Task Save(Account account);
}
