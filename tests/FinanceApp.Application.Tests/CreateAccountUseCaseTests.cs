using FinanceApp.Application.Account;
using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.ValueObjects;
using FluentAssertions;
using Moq;

using AccountEntity = FinanceApp.Domain.Account.Account;
using AccountRepo = FinanceApp.Domain.Account.IAccountRepository;

namespace FinanceApp.Application.Tests;

public class CreateAccountUseCaseTests
{
    private readonly Mock<AccountRepo> _accountRepoMock = new();
    private readonly CreateAccountUseCase _useCase;

    public CreateAccountUseCaseTests()
    {
        _useCase = new CreateAccountUseCase(_accountRepoMock.Object);
    }

    [Fact]
    public async Task DeveCriarConta_QuandoDocumentoNaoExistir()
    {
        // Arrange
        _accountRepoMock.Setup(r => r.GetByDocument("12345678900")).ReturnsAsync((AccountEntity?)null);
        _accountRepoMock.Setup(r => r.Save(It.IsAny<AccountEntity>())).Returns(Task.CompletedTask);

        // Act
        var result = await _useCase.ExecuteAsync("João", "12345678900");

        // Assert
        result.Should().NotBeNull();
        result.Document.Should().Be("12345678900");
        _accountRepoMock.Verify(r => r.Save(It.IsAny<AccountEntity>()), Times.Once);
    }

    [Fact]
    public async Task DeveLancarExcecao_QuandoDocumentoJaExistir()
    {
        // Arrange
        var existingAccount = new AccountEntity(Guid.NewGuid(), "João", new Document("12345678900"), new Money(100m));
        _accountRepoMock.Setup(r => r.GetByDocument("12345678900")).ReturnsAsync(existingAccount);

        // Act
        var act = async () => await _useCase.ExecuteAsync("Maria", "12345678900");

        // Assert
        await act.Should().ThrowAsync<AccountException>()
            .WithMessage("*documento*");
    }
}
