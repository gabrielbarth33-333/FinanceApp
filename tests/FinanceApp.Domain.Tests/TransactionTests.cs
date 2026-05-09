using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.ValueObjects;
using FluentAssertions;

using TransactionEntity = FinanceApp.Domain.Transaction.Transaction;
using TransactionType = FinanceApp.Domain.Transaction.TransactionType;

namespace FinanceApp.Domain.Tests;

public class TransactionTests
{
    private static CategoryRef ValidCategory() => new(Guid.NewGuid(), "Alimentação");

    [Fact]
    public void DeveCriarTransacao_QuandoDadosValidos()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var date = DateTime.UtcNow.AddMinutes(-1);

        // Act
        var transaction = new TransactionEntity(accountId, date, ValidCategory(), TransactionType.Income, new Money(100m));

        // Assert
        transaction.AccountId.Should().Be(accountId);
        transaction.Amount.Value.Should().Be(100m);
        transaction.Type.Should().Be(TransactionType.Income);
    }

    [Fact]
    public void DeveLancarTransactionException_QuandoAccountIdForVazio()
    {
        // Arrange & Act
        var act = () => new TransactionEntity(Guid.Empty, DateTime.UtcNow, ValidCategory(), TransactionType.Income, new Money(100m));

        // Assert
        act.Should().Throw<TransactionException>()
           .WithMessage("*AccountId*");
    }

    [Fact]
    public void DeveLancarTransactionException_QuandoDataForFutura()
    {
        // Arrange & Act
        var act = () => new TransactionEntity(Guid.NewGuid(), DateTime.UtcNow.AddDays(1), ValidCategory(), TransactionType.Income, new Money(100m));

        // Assert
        act.Should().Throw<TransactionException>()
           .WithMessage("*futura*");
    }

    [Fact]
    public void DeveLancarDomainException_QuandoValorForNegativo()
    {
        // Arrange & Act
        var act = () => new TransactionEntity(Guid.NewGuid(), DateTime.UtcNow, ValidCategory(), TransactionType.Expense, new Money(-100m));

        // Assert
        act.Should().Throw<DomainException>();
    }
}
