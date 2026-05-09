using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.ValueObjects;

namespace FinanceApp.Domain.Transaction;

// [SOLID: SRP] — Transaction representa e valida apenas a movimentação financeira.
// Não conhece Account por objeto — apenas por ID. Não sabe nada de persistência.
//
// [GRASP: Low Coupling — Modelo B] — AccountId: Guid em vez de Account object.
// Objetivo: minimizar o acoplamento entre agregados. Transaction não precisa carregar
// Account na memória; o vínculo é por identidade, não por referência direta.
// Isso evita carregamento em cascata e mantém os agregados independentes.
/// <summary> <<AggregateRoot>> Movimentação financeira (Income ou Expense). Referencia Account apenas por AccountId. </summary>
public class Transaction
{
    public Guid Id { get; private set; }

    // [GRASP: Low Coupling] — AccountId (Guid) em vez de Account (objeto).
    // Transaction nunca "puxa" Account do banco por navegação — responsabilidade do UseCase.
    public Guid AccountId { get; private set; }
    public DateTime Date { get; private set; }
    public CategoryRef CategoryRef { get; private set; }
    public TransactionType Type { get; private set; }
    public Money Amount { get; private set; }

    public Transaction(Guid accountId, DateTime date, CategoryRef categoryRef, TransactionType type, Money amount)
    {
        if (accountId == Guid.Empty)
            throw new TransactionException("AccountId inválido.");

        if (date > DateTime.UtcNow)
            throw new TransactionException("A data da transação não pode ser futura.");

        // [GRASP: Expert] — Transaction conhece suas próprias invariantes e as aplica no construtor.
        // Nenhum código externo pode criar uma Transaction inválida.
        Id = Guid.NewGuid();
        AccountId = accountId;
        Date = date;
        CategoryRef = categoryRef;
        Type = type;
        Amount = amount;
    }

    // Construtor sem parâmetros para o EF Core
    private Transaction() { Id = Guid.Empty; AccountId = Guid.Empty; Date = default; CategoryRef = null!; Amount = null!; }
}
