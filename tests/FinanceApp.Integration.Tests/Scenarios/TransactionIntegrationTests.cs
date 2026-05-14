using FinanceApp.Domain.Transaction;
using FinanceApp.Integration.Tests.Fixtures;
using System.Globalization;
using System.Text.Json.Serialization;

namespace FinanceApp.Integration.Tests.Scenarios;

/// <summary>
/// [SOLID: SRP] — Suite de testes de integração para o Agregado Transaction.
/// Testa fluxos reais: API → Application → Domain → Infrastructure → PostgreSQL.
/// Não usa Mocks — apenas o banco de testes (Testcontainers).
///
/// [GRASP: Controller] — Usa ApiFixture para delegar requisições HTTP e verificar respostas reais.
/// </summary>
public class TransactionIntegrationTests : IAsyncLifetime
{
    private readonly DatabaseFixture _databaseFixture = new();
    private ApiFixture _apiFixture = null!;

    public async Task InitializeAsync()
    {
        await _databaseFixture.InitializeAsync();
        _apiFixture = new ApiFixture(_databaseFixture);
        _apiFixture.Initialize();
    }

    public async Task DisposeAsync()
    {
        await _apiFixture.DisposeAsync();
        await _databaseFixture.ResetAsync();
        await _databaseFixture.DisposeAsync();
    }

    [Fact]
    public async Task DeveRegistrarTransacao_ComCategoriaValida()
    {
        // Arrange — criar conta primeiro
        var accountRequest = new CreateAccountRequest
        {
            Name = "João Silva",
            Document = "12345678900",
            InitialBalance = 5000.00m
        };

        await _apiFixture.PostAsync<AccountResponse>("/api/accounts", accountRequest);

        // Registrar transação de despesa com categoria válida
        var transactionRequest = new RegisterTransactionRequest
        {
            Document = "12345678900",
            Amount = 150.00m,
            Type = "Expense",
            CategoryName = "Alimentação",
            Date = DateTime.UtcNow
        };

        // Act
        var (statusCode, _) = await _apiFixture.PostAsync<object>(
            "/api/transactions",
            transactionRequest);

        // Assert
        statusCode.Should().Be(201); // Created
    }

    [Fact]
    public async Task DeveRegistrarTransacao_ComCategoryCase_Insensitive()
    {
        // Arrange — criar conta
        var accountRequest = new CreateAccountRequest
        {
            Name = "Maria Santos",
            Document = "98765432100",
            InitialBalance = 5000.00m
        };

        await _apiFixture.PostAsync<AccountResponse>("/api/accounts", accountRequest);

        // Registrar com caso diferente da categoria padrão
        var transactionRequest = new RegisterTransactionRequest
        {
            Document = "98765432100",
            Amount = 200.00m,
            Type = "Income",
            CategoryName = "ALIMENTACAO", // uppercase
            Date = DateTime.UtcNow
        };

        // Act
        var (statusCode, _) = await _apiFixture.PostAsync<object>(
            "/api/transactions",
            transactionRequest);

        // Assert
        statusCode.Should().Be(201); // Case-insensitive search funciona via ACL
    }

    [Fact]
    public async Task DeveRegistrarTransacao_ComCategoryAcento_Insensitive()
    {
        // Arrange — criar conta
        var accountRequest = new CreateAccountRequest
        {
            Name = "Pedro Costa",
            Document = "11111111111",
            InitialBalance = 5000.00m
        };

        await _apiFixture.PostAsync<AccountResponse>("/api/accounts", accountRequest);

        // Registrar sem acento (categoria padrão é "Saúde")
        var transactionRequest = new RegisterTransactionRequest
        {
            Document = "11111111111",
            Amount = 300.00m,
            Type = "Expense",
            CategoryName = "saude", // sem acento
            Date = DateTime.UtcNow
        };

        // Act
        var (statusCode, _) = await _apiFixture.PostAsync<object>(
            "/api/transactions",
            transactionRequest);

        // Assert
        statusCode.Should().Be(201); // Acento-insensitive search funciona via ACL
    }

    [Fact]
    public async Task DeveListarTransacoes_ComFiltroDePeriodo()
    {
        // Arrange — criar conta
        var accountRequest = new CreateAccountRequest
        {
            Name = "Alice",
            Document = "22222222222",
            InitialBalance = 10000.00m
        };

        await _apiFixture.PostAsync<AccountResponse>("/api/accounts", accountRequest);

        // Registrar 2 transações em datas diferentes
        var today = DateTime.UtcNow;
        var oneWeekAgo = today.AddDays(-7);

        var tx1 = new RegisterTransactionRequest
        {
            Document = "22222222222",
            Amount = 100m,
            Type = "Expense",
            CategoryName = "Alimentação",
            Date = oneWeekAgo
        };

        var tx2 = new RegisterTransactionRequest
        {
            Document = "22222222222",
            Amount = 200m,
            Type = "Income",
            CategoryName = "Salário",
            Date = today
        };

        await _apiFixture.PostAsync<object>("/api/transactions", tx1);
        await _apiFixture.PostAsync<object>("/api/transactions", tx2);

        // Act — listar com período (últimos 10 dias)
        var startDate = today.AddDays(-10).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var endDate = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        var result = await _apiFixture.GetAsync<ListTransactionsResponse>(
            $"/api/transactions/22222222222?startDate={startDate}&endDate={endDate}");

        // Assert
        result.Should().NotBeNull();
        result!.Transactions.Should().HaveCount(2);
        // API retorna em formato ISO (ex: 2026-05-04T00:00:00Z), extrair apenas data
        result.StartDate.Should().StartWith(startDate);
        result.EndDate.Should().StartWith(endDate);
    }

    [Fact]
    public async Task DeveCalcularBalanco_Corretamente()
    {
        // Arrange — criar conta com saldo inicial
        var accountRequest = new CreateAccountRequest
        {
            Name = "Bob",
            Document = "33333333333",
            InitialBalance = 5000m
        };

        await _apiFixture.PostAsync<AccountResponse>("/api/accounts", accountRequest);

        // Registrar receita e despesa
        var income = new RegisterTransactionRequest
        {
            Document = "33333333333",
            Amount = 1000m,
            Type = "Income",
            CategoryName = "Salário",
            Date = DateTime.UtcNow
        };

        var expense = new RegisterTransactionRequest
        {
            Document = "33333333333",
            Amount = 200m,
            Type = "Expense",
            CategoryName = "Alimentação",
            Date = DateTime.UtcNow
        };

        await _apiFixture.PostAsync<object>("/api/transactions", income);
        await _apiFixture.PostAsync<object>("/api/transactions", expense);

        // Act — listar transações
        var result = await _apiFixture.GetAsync<ListTransactionsResponse>(
            "/api/transactions/33333333333");

        // Assert
        result.Should().NotBeNull();
        result!.TotalIncome.Should().Be(1000m);    // Income
        result.TotalExpense.Should().Be(200m);     // Expense
        result.NetBalance.Should().Be(800m);       // Income - Expense
    }

    [Fact]
    public async Task DeveRetornarErro_AoRegistrarComCategoriaInexistente()
    {
        // Arrange — criar conta
        var accountRequest = new CreateAccountRequest
        {
            Name = "Charlie",
            Document = "44444444444",
            InitialBalance = 5000m
        };

        await _apiFixture.PostAsync<AccountResponse>("/api/accounts", accountRequest);

        // Tentar registrar com categoria inexistente
        var transactionRequest = new RegisterTransactionRequest
        {
            Document = "44444444444",
            Amount = 100m,
            Type = "Expense",
            CategoryName = "CategoriaQueNaoExiste",
            Date = DateTime.UtcNow
        };

        // Act
        var (statusCode, _) = await _apiFixture.PostAsync<object>(
            "/api/transactions",
            transactionRequest);

        // Assert
        statusCode.Should().Be(404); // CategoryException mapeia para 404
    }

    [Fact]
    public async Task DeveRetornarErro_AoRegistrarComDocumentoInexistente()
    {
        // Arrange — não criar conta

        // Tentar registrar transação com documento inexistente
        var transactionRequest = new RegisterTransactionRequest
        {
            Document = "99999999999",
            Amount = 100m,
            Type = "Expense",
            CategoryName = "Alimentação",
            Date = DateTime.UtcNow
        };

        // Act
        var (statusCode, _) = await _apiFixture.PostAsync<object>(
            "/api/transactions",
            transactionRequest);

        // Assert
        statusCode.Should().Be(404); // AccountException mapeia para 404
    }

    [Fact]
    public async Task DeveRetornarBalancoPadrao_QuandoNaoInformarPeriodo()
    {
        // Arrange — criar conta e registrar transações
        var accountRequest = new CreateAccountRequest
        {
            Name = "Dana",
            Document = "55555555555",
            InitialBalance = 5000m
        };

        await _apiFixture.PostAsync<AccountResponse>("/api/accounts", accountRequest);

        var transactionRequest = new RegisterTransactionRequest
        {
            Document = "55555555555",
            Amount = 100m,
            Type = "Expense",
            CategoryName = "Alimentação",
            Date = DateTime.UtcNow
        };

        await _apiFixture.PostAsync<object>("/api/transactions", transactionRequest);

        // Act — listar sem especificar período
        var result = await _apiFixture.GetAsync<ListTransactionsResponse>(
            "/api/transactions/55555555555");

        // Assert — deve retornar período padrão (últimos 30 dias)
        result.Should().NotBeNull();
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var expectedStart = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        
        // API retorna em formato ISO, comparar apenas prefixo da data
        result!.EndDate.Should().StartWith(today);
        result.StartDate.Should().StartWith(expectedStart);
        result.Transactions.Should().HaveCount(1);
    }

    // ==================== DTOs de Requisição ====================

    private class CreateAccountRequest
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;

        [JsonPropertyName("document")]
        public string Document { get; set; } = null!;

        [JsonPropertyName("initialBalance")]
        public decimal InitialBalance { get; set; }
    }

    private class RegisterTransactionRequest
    {
        [JsonPropertyName("document")]
        public string Document { get; set; } = null!;

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = null!;

        [JsonPropertyName("categoryName")]
        public string CategoryName { get; set; } = null!;

        [JsonPropertyName("date")]
        public DateTime? Date { get; set; }
    }

    // ==================== DTOs de Resposta ====================

    private class AccountResponse
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;

        [JsonPropertyName("document")]
        public string Document { get; set; } = null!;

        [JsonPropertyName("balance")]
        public MoneyResponse Balance { get; set; } = null!;
    }

    private class MoneyResponse
    {
        [JsonPropertyName("value")]
        public decimal Value { get; set; }
    }

    private class ListTransactionsResponse
    {
        [JsonPropertyName("startDate")]
        public string StartDate { get; set; } = null!;

        [JsonPropertyName("endDate")]
        public string EndDate { get; set; } = null!;

        [JsonPropertyName("transactions")]
        public List<TransactionItemResponse> Transactions { get; set; } = new();

        [JsonPropertyName("totalIncome")]
        public decimal TotalIncome { get; set; }

        [JsonPropertyName("totalExpense")]
        public decimal TotalExpense { get; set; }

        [JsonPropertyName("netBalance")]
        public decimal NetBalance { get; set; }
    }

    private class TransactionItemResponse
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("document")]
        public string Document { get; set; } = null!;

        [JsonPropertyName("amount")]
        public MoneyResponse Amount { get; set; } = null!;

        [JsonPropertyName("type")]
        public string Type { get; set; } = null!;

        [JsonPropertyName("categoryName")]
        public string CategoryName { get; set; } = null!;

        [JsonPropertyName("registeredAt")]
        public DateTime RegisteredAt { get; set; }
    }
}
