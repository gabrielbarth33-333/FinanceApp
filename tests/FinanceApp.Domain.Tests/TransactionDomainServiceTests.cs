using FinanceApp.Domain.Account;
using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.Transaction;
using FinanceApp.Domain.ValueObjects;
using FluentAssertions;
using Moq;

using AccountEntity = FinanceApp.Domain.Account.Account;

namespace FinanceApp.Domain.Tests;

public class TransactionDomainServiceTests
{
    private readonly Mock<IAccountRepository> _repoMock = new();
    private readonly TransactionDomainService _service;

    public TransactionDomainServiceTests()
    {
        _service = new TransactionDomainService(_repoMock.Object);
    }

    [Fact]
    public async Task DevePassar_QuandoContaExisteESaldoSuficiente()
    {
        // Arrange
        var id = Guid.NewGuid();
        var account = new AccountEntity(id, "João", "12345678900", new Money(500m));
        _repoMock.Setup(r => r.GetById(id)).ReturnsAsync(account);

        // Act
        var act = async () => await _service.ValidateBalance(id, new Money(100m));

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeveLancarAccountException_QuandoContaNaoExistir()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetById(id)).ReturnsAsync((AccountEntity?)null);

        // Act
        var act = async () => await _service.ValidateBalance(id, new Money(100m));

        // Assert
        await act.Should().ThrowAsync<AccountException>()
            .WithMessage($"*{id}*");
    }

    [Fact]
    public async Task DeveLancarTransactionException_QuandoSaldoInsuficiente()
    {
        // Arrange
        var id = Guid.NewGuid();
        var account = new AccountEntity(id, "Maria", "11122233344", new Money(50m));
        _repoMock.Setup(r => r.GetById(id)).ReturnsAsync(account);

        // Act
        var act = async () => await _service.ValidateBalance(id, new Money(200m));

        // Assert
        await act.Should().ThrowAsync<TransactionException>()
            .WithMessage("*Saldo insuficiente*");
    }
}
