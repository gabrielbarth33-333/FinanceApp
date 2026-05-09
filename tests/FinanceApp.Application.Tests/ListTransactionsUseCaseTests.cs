using FinanceApp.Application.Transaction;
using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.Transaction;
using FinanceApp.Domain.ValueObjects;
using FluentAssertions;
using Moq;

using AccountEntity = FinanceApp.Domain.Account.Account;
using AccountRepo = FinanceApp.Domain.Account.IAccountRepository;
using TransactionEntity = FinanceApp.Domain.Transaction.Transaction;

namespace FinanceApp.Application.Tests;

public class ListTransactionsUseCaseTests
{
    private const string Document = "12345678900";

    private readonly Mock<AccountRepo> _accountRepoMock = new();
    private readonly Mock<ITransactionRepository> _transactionRepoMock = new();
    private readonly ListTransactionsUseCase _useCase;

    public ListTransactionsUseCaseTests()
    {
        _useCase = new ListTransactionsUseCase(
            _accountRepoMock.Object,
            _transactionRepoMock.Object);
    }

    [Fact]
    public async Task DeveRetornarBalanceDoPeriodo_QuandoTransacoesExistirem()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = new AccountEntity(accountId, "João", Document, new Money(500m));
        var categoryRef = new CategoryRef(Guid.NewGuid(), "Salário");

        var transactions = new List<TransactionEntity>
        {
            new(accountId, DateTime.UtcNow.AddDays(-5), categoryRef, TransactionType.Income, new Money(1000m)),
            new(accountId, DateTime.UtcNow.AddDays(-3), new CategoryRef(Guid.NewGuid(), "Alimentação"), TransactionType.Expense, new Money(200m)),
            new(accountId, DateTime.UtcNow.AddDays(-1), new CategoryRef(Guid.NewGuid(), "Transporte"), TransactionType.Expense, new Money(50m)),
        };

        _accountRepoMock.Setup(r => r.GetByDocument(Document)).ReturnsAsync(account);
        _transactionRepoMock
            .Setup(r => r.ListByAccountIdAndPeriod(accountId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(transactions);

        // Act
        var result = await _useCase.ExecuteAsync(Document);

        // Assert
        result.TotalIncome.Should().Be(1000m);
        result.TotalExpense.Should().Be(250m);
        result.NetBalance.Should().Be(750m);
        result.Transactions.Should().HaveCount(3);
    }

    [Fact]
    public async Task DeveAplicarUltimoMes_QuandoPeriodoNaoForInformado()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = new AccountEntity(accountId, "João", Document, new Money(100m));
        var expectedStart = DateTime.UtcNow.Date.AddMonths(-1);

        _accountRepoMock.Setup(r => r.GetByDocument(Document)).ReturnsAsync(account);
        _transactionRepoMock
            .Setup(r => r.ListByAccountIdAndPeriod(accountId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync([]);

        // Act
        var result = await _useCase.ExecuteAsync(Document, startDate: null, endDate: null);

        // Assert
        result.StartDate.Date.Should().Be(expectedStart);
        result.EndDate.Date.Should().Be(DateTime.UtcNow.Date);
    }

    [Fact]
    public async Task DeveTruncarHorario_QuandoDatasForemInformadas()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = new AccountEntity(accountId, "João", Document, new Money(100m));
        var startWithTime = new DateTime(2024, 1, 10, 15, 30, 0);
        var endWithTime = new DateTime(2024, 2, 10, 23, 59, 59);

        DateTime capturedStart = default, capturedEnd = default;

        _accountRepoMock.Setup(r => r.GetByDocument(Document)).ReturnsAsync(account);
        _transactionRepoMock
            .Setup(r => r.ListByAccountIdAndPeriod(accountId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .Callback<Guid, DateTime, DateTime>((_, s, e) => { capturedStart = s; capturedEnd = e; })
            .ReturnsAsync([]);

        // Act
        await _useCase.ExecuteAsync(Document, startWithTime, endWithTime);

        // Assert — hora deve ser zerada
        capturedStart.TimeOfDay.Should().Be(TimeSpan.Zero);
        capturedEnd.Date.Should().Be(endWithTime.Date);
    }

    [Fact]
    public async Task DeveLancarExcecao_QuandoDocumentoNaoExistir()
    {
        // Arrange
        _accountRepoMock.Setup(r => r.GetByDocument(Document)).ReturnsAsync((AccountEntity?)null);

        // Act
        var act = async () => await _useCase.ExecuteAsync(Document);

        // Assert
        await act.Should().ThrowAsync<AccountException>()
            .WithMessage($"*{Document}*");
    }

    [Fact]
    public async Task DeveRetornarBalanceZero_QuandoNaoHouverTransacoes()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = new AccountEntity(accountId, "João", Document, new Money(100m));

        _accountRepoMock.Setup(r => r.GetByDocument(Document)).ReturnsAsync(account);
        _transactionRepoMock
            .Setup(r => r.ListByAccountIdAndPeriod(accountId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync([]);

        // Act
        var result = await _useCase.ExecuteAsync(Document);

        // Assert
        result.TotalIncome.Should().Be(0);
        result.TotalExpense.Should().Be(0);
        result.NetBalance.Should().Be(0);
        result.Transactions.Should().BeEmpty();
    }
}
