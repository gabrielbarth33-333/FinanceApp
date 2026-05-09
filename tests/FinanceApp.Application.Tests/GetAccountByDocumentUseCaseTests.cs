using FinanceApp.Application.Account;
using FinanceApp.Domain.Account;
using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.ValueObjects;
using FluentAssertions;
using Moq;

using AccountEntity = FinanceApp.Domain.Account.Account;

namespace FinanceApp.Application.Tests;

public class GetAccountByDocumentUseCaseTests
{
    private readonly Mock<IAccountRepository> _repoMock = new();
    private readonly GetAccountByDocumentUseCase _useCase;

    public GetAccountByDocumentUseCaseTests()
    {
        _useCase = new GetAccountByDocumentUseCase(_repoMock.Object);
    }

    [Fact]
    public async Task DeveRetornarConta_QuandoDocumentoExistir()
    {
        // Arrange
        const string document = "98765432100";
        var account = new AccountEntity(Guid.NewGuid(), "Carlos", document, new Money(200m));
        _repoMock.Setup(r => r.GetByDocument(document)).ReturnsAsync(account);

        // Act
        var result = await _useCase.ExecuteAsync(document);

        // Assert
        result.Should().NotBeNull();
        result.Document.Should().Be(document);
    }

    [Fact]
    public async Task DeveLancarAccountException_QuandoDocumentoNaoExistir()
    {
        // Arrange
        const string document = "00000000000";
        _repoMock.Setup(r => r.GetByDocument(document)).ReturnsAsync((AccountEntity?)null);

        // Act
        var act = async () => await _useCase.ExecuteAsync(document);

        // Assert
        await act.Should().ThrowAsync<AccountException>()
            .WithMessage($"*{document}*");
    }
}
