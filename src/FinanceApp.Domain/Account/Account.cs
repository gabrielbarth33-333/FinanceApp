using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.ValueObjects;

namespace FinanceApp.Domain.Account;

// [SOLID: SRP] — Account tem uma única responsabilidade: representar e proteger o estado
// de uma conta financeira. Não sabe nada sobre persistência, HTTP ou casos de uso.
/// <summary> <<AggregateRoot>> Conta financeira do usuário. </summary>
public class Account
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Document { get; private set; }
    public Money Balance { get; private set; }

    public Account(Guid id, string name, Document document, Money balance)
    {
        if (id == Guid.Empty)
            throw new AccountException("Id da conta inválido.");

        if (string.IsNullOrWhiteSpace(name))
            throw new AccountException("Nome é obrigatório.");

        if (document?.Value == null)
            throw new AccountException("Documento é obrigatório.");

        Id = id;
        Name = name;
        Document = document.Value; // Extrai o valor sanitizado do value object
        Balance = balance;
    }

    // Construtor sem parâmetros para o EF Core
    private Account() { Id = Guid.Empty; Name = string.Empty; Document = string.Empty; Balance = null!; }

    // [GRASP: Expert] — HasBalance está em Account porque Account é quem possui a informação
    // do saldo. Ela é a "especialista" para responder se pode cobrir um valor.
    // Objetivo: atribuir a responsabilidade à classe que detém os dados necessários.
    /// <summary> Verifica se a conta possui saldo suficiente para cobrir o valor informado. </summary>
    public bool HasBalance(Money amount) => Balance.Value >= amount.Value;

    // [GRASP: Expert] — UpdateBalance está em Account pela mesma razão: somente Account
    // conhece as regras de como seu próprio saldo deve mudar após uma transação.
    //
    // [SOLID: SRP] — A atualização do saldo é responsabilidade do agregado, não do UseCase.
    // O UseCase apenas orquestra; Account decide como seu estado muda.
    /// <summary> Atualiza o saldo após o registro de uma transação. </summary>
    public void UpdateBalance(Money amount, Transaction.TransactionType type)
    {
        var newValue = type == Transaction.TransactionType.Income
            ? Balance.Value + amount.Value
            : Balance.Value - amount.Value;

        if (newValue < 0)
            throw new AccountException("Saldo insuficiente para realizar a operação.");

        Balance = new Money(newValue);
    }
}
