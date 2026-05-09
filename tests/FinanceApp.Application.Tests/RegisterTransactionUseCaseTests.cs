using FinanceApp.Application.Interfaces;
using FinanceApp.Application.Transaction;
using FinanceApp.Catalog.Exceptions;
using FinanceApp.Domain.ACL;
using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.Transaction;
using FinanceApp.Domain.ValueObjects;
using FluentAssertions;
using Moq;

using AccountEntity = FinanceApp.Domain.Account.Account;
using AccountRepo = FinanceApp.Domain.Account.IAccountRepository;
using TransactionEntity = FinanceApp.Domain.Transaction.Transaction;

namespace FinanceApp.Application.Tests;

public class RegisterTransactionUseCaseTests
{
    private const string Document = "12345678900";

    private readonly Mock<AccountRepo> _accountRepoMock = new();
    private readonly Mock<ITransactionRepository> _transactionRepoMock = new();
    private readonly Mock<ICategoryACL> _categoryAclMock = new();
    private readonly Mock<ITransactionDomainService> _domainServiceMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly RegisterTransactionUseCase _useCase;

    public RegisterTransactionUseCaseTests()
    {
        _unitOfWorkMock.Setup(u => u.BeginAsync()).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.RollbackAsync()).Returns(Task.CompletedTask);

        _useCase = new RegisterTransactionUseCase(
            _accountRepoMock.Object,
            _transactionRepoMock.Object,
            _categoryAclMock.Object,
            _domainServiceMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task DeveRegistrarTransacao_ComSucesso()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = new AccountEntity(accountId, "João", new Document(Document), new Money(1000m));
        var categoryRef = new CategoryRef(Guid.NewGuid(), "Alimentação");

        _accountRepoMock.Setup(r => r.GetByDocument(Document)).ReturnsAsync(account);
        _categoryAclMock.Setup(a => a.GetCategory("Alimentação")).ReturnsAsync(categoryRef);
        _transactionRepoMock.Setup(r => r.Add(It.IsAny<TransactionEntity>())).Returns(Task.CompletedTask);
        _accountRepoMock.Setup(r => r.Save(It.IsAny<AccountEntity>())).Returns(Task.CompletedTask);
        _domainServiceMock.Setup(d => d.ValidateBalance(accountId, It.IsAny<Money>())).Returns(Task.CompletedTask);

        // Act — date null → usa DateTime.UtcNow
        await _useCase.ExecuteAsync(Document, 100m, TransactionType.Expense, "Alimentação", date: null);

        // Assert
        _transactionRepoMock.Verify(r => r.Add(It.IsAny<TransactionEntity>()), Times.Once);
        _accountRepoMock.Verify(r => r.Save(It.IsAny<AccountEntity>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task DeveLancarExcecao_QuandoDocumentoNaoExistir()
    {
        // Arrange
        _accountRepoMock.Setup(r => r.GetByDocument(Document)).ReturnsAsync((AccountEntity?)null);

        // Act
        var act = async () => await _useCase.ExecuteAsync(Document, 100m, TransactionType.Expense, "Alimentação");

        // Assert
        await act.Should().ThrowAsync<AccountException>()
            .WithMessage($"*{Document}*");
        _unitOfWorkMock.Verify(u => u.BeginAsync(), Times.Never);
    }

    [Fact]
    public async Task DeveLancarExcecao_QuandoSaldoForInsuficiente()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = new AccountEntity(accountId, "João", new Document(Document), new Money(50m));
        var categoryRef = new CategoryRef(Guid.NewGuid(), "Alimentação");

        _accountRepoMock.Setup(r => r.GetByDocument(Document)).ReturnsAsync(account);
        _categoryAclMock.Setup(a => a.GetCategory("Alimentação")).ReturnsAsync(categoryRef);
        _domainServiceMock.Setup(d => d.ValidateBalance(accountId, It.IsAny<Money>()))
            .ThrowsAsync(new TransactionException("Saldo insuficiente para realizar a transação."));

        // Act
        var act = async () => await _useCase.ExecuteAsync(Document, 500m, TransactionType.Expense, "Alimentação");

        // Assert
        await act.Should().ThrowAsync<TransactionException>()
            .WithMessage("*Saldo insuficiente*");
    }

    [Fact]
    public async Task DeveLancarExcecao_QuandoCategoriaForInvalida()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = new AccountEntity(accountId, "João", new Document(Document), new Money(1000m));

        _accountRepoMock.Setup(r => r.GetByDocument(Document)).ReturnsAsync(account);
        _categoryAclMock.Setup(a => a.GetCategory("Inexistente"))
            .ThrowsAsync(new CategoryException("Categoria 'Inexistente' não encontrada."));
        _domainServiceMock.Setup(d => d.ValidateBalance(accountId, It.IsAny<Money>())).Returns(Task.CompletedTask);

        // Act
        var act = async () => await _useCase.ExecuteAsync(Document, 100m, TransactionType.Expense, "Inexistente");

        // Assert
        await act.Should().ThrowAsync<CategoryException>();
    }

    [Fact]
    public async Task DeveUsarDataAtual_QuandoDateForNula()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = new AccountEntity(accountId, "João", new Document(Document), new Money(1000m));
        var categoryRef = new CategoryRef(Guid.NewGuid(), "Alimentação");
        TransactionEntity? capturedTransaction = null;

        _accountRepoMock.Setup(r => r.GetByDocument(Document)).ReturnsAsync(account);
        _categoryAclMock.Setup(a => a.GetCategory("Alimentação")).ReturnsAsync(categoryRef);
        _transactionRepoMock.Setup(r => r.Add(It.IsAny<TransactionEntity>()))
            .Callback<TransactionEntity>(t => capturedTransaction = t)
            .Returns(Task.CompletedTask);
        _accountRepoMock.Setup(r => r.Save(It.IsAny<AccountEntity>())).Returns(Task.CompletedTask);
        _domainServiceMock.Setup(d => d.ValidateBalance(accountId, It.IsAny<Money>())).Returns(Task.CompletedTask);

        var before = DateTime.UtcNow;

        // Act
        await _useCase.ExecuteAsync(Document, 100m, TransactionType.Expense, "Alimentação", date: null);

        // Assert
        capturedTransaction!.Date.Should().BeOnOrAfter(before).And.BeOnOrBefore(DateTime.UtcNow);
    }
}