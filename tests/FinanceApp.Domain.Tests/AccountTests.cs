using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.Transaction;
using FinanceApp.Domain.ValueObjects;
using FluentAssertions;

using AccountEntity = FinanceApp.Domain.Account.Account;

namespace FinanceApp.Domain.Tests;

public class AccountTests
{
    private static AccountEntity CreateAccount(decimal balance = 1000m) =>
        new(Guid.NewGuid(), "João Silva", "12345678900", new Money(balance));

    [Fact]
    public void DeveAtualizarSaldo_QuandoTransacaoDeReceitaForRegistrada()
    {
        // Arrange
        var account = CreateAccount(500m);

        // Act
        account.UpdateBalance(new Money(200m), TransactionType.Income);

        // Assert
        account.Balance.Value.Should().Be(700m);
    }

    [Fact]
    public void DeveAtualizarSaldo_QuandoTransacaoDeDespesaForRegistrada()
    {
        // Arrange
        var account = CreateAccount(500m);

        // Act
        account.UpdateBalance(new Money(200m), TransactionType.Expense);

        // Assert
        account.Balance.Value.Should().Be(300m);
    }

    [Fact]
    public void DeveLancarExcecao_QuandoSaldoForInsuficienteParaDespesa()
    {
        // Arrange
        var account = CreateAccount(100m);

        // Act
        var act = () => account.UpdateBalance(new Money(200m), TransactionType.Expense);

        // Assert
        act.Should().Throw<AccountException>()
           .WithMessage("*Saldo insuficiente*");
    }

    [Fact]
    public void DeveRetornarTrue_QuandoContaTemSaldoSuficiente()
    {
        // Arrange
        var account = CreateAccount(500m);

        // Act & Assert
        account.HasBalance(new Money(300m)).Should().BeTrue();
    }

    [Fact]
    public void DeveRetornarFalse_QuandoContaNaoTemSaldoSuficiente()
    {
        // Arrange
        var account = CreateAccount(100m);

        // Act & Assert
        account.HasBalance(new Money(200m)).Should().BeFalse();
    }

    [Fact]
    public void DeveLancarAccountException_QuandoIdForVazio()
    {
        // Arrange & Act
        var act = () => new AccountEntity(Guid.Empty, "João", "12345678900", new Money(100m));

        // Assert
        act.Should().Throw<AccountException>();
    }
}
