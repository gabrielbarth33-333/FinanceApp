using FinanceApp.Application.Account;
using FinanceApp.Domain.Account;
using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.Transaction;
using FinanceApp.Domain.ValueObjects;
using FluentAssertions;
using Moq;

using AccountEntity = FinanceApp.Domain.Account.Account;

namespace FinanceApp.Application.Tests;

public class GetAccountUseCaseTests
{
    private readonly Mock<IAccountRepository> _repoMock = new();
    private readonly GetAccountUseCase _useCase;

    public GetAccountUseCaseTests()
    {
        _useCase = new GetAccountUseCase(_repoMock.Object);
    }

    [Fact]
    public async Task DeveRetornarConta_QuandoIdExistir()
    {
        // Arrange
        var id = Guid.NewGuid();
        var account = new AccountEntity(id, "Ana", new Document("12345678900"), new Money(500m));
        _repoMock.Setup(r => r.GetById(id)).ReturnsAsync(account);

        // Act
        var result = await _useCase.ExecuteAsync(id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(id);
    }

    [Fact]
    public async Task DeveLancarAccountException_QuandoIdNaoExistir()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetById(id)).ReturnsAsync((AccountEntity?)null);

        // Act
        var act = async () => await _useCase.ExecuteAsync(id);

        // Assert
        await act.Should().ThrowAsync<AccountException>()
            .WithMessage($"*{id}*");
    }
}
