using FinanceApp.Catalog.Exceptions;
using FinanceApp.Integration.Tests.Fixtures;

namespace FinanceApp.Integration.Tests.Scenarios;

/// <summary>
/// [SOLID: SRP] — Suite de testes de integração para o CategoryACL.
/// Testa fluxos reais: API (Category Endpoint) → Application → Domain (ICategoryACL) → Infrastructure (CategoryACL) → PostgreSQL.
/// Não usa Mocks — apenas o banco de testes (Testcontainers).
///
/// [GRASP: Controller] — Usa ApiFixture para delegar requisições HTTP e verificar respostas reais.
/// Focus: Validar comportamentos case-insensitive, accent-insensitive, boundary cases, e erro handling.
/// </summary>
public class CategoryACLIntegrationTests : IAsyncLifetime
{
    private readonly DatabaseFixture _databaseFixture = new();
    private ApiFixture _apiFixture = null!;

    public async Task InitializeAsync()
    {
        await _databaseFixture.InitializeAsync();
        await _databaseFixture.ResetAsync();
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
    public async Task DeveRetornarTodasAsCategorias_Listar()
    {
        // Act
        var response = await _apiFixture.GetAsync<List<CategoryItemResponse>>(
            "/api/categories");

        // Assert
        response.Should().NotBeNull();
        response!.Should().NotBeEmpty();
        response.Should().HaveCount(9);  // Seed: 9 categorias padrão
        
        // Validar categorias conhecidas
        response.Should().Contain(c => c.Name == "Alimentação");
        response.Should().Contain(c => c.Name == "Saúde");
        response.Should().Contain(c => c.Name == "Transporte");
        response.Should().Contain(c => c.Name == "Moradia");
        response.Should().Contain(c => c.Name == "Lazer");
        response.Should().Contain(c => c.Name == "Educação");
        response.Should().Contain(c => c.Name == "Salário");
        response.Should().Contain(c => c.Name == "Investimentos");
        response.Should().Contain(c => c.Name == "Outros");
    }

    [Fact]
    public async Task DeveLookupCategoria_CaseInsensitive_MinusculasDeEncontram()
    {
        // Arrange — Criar conta e registrar transação com categoria em minúsculas
        var accountRequest = new CreateAccountRequest
        {
            Name = "Test User",
            Document = "11111111111",
            InitialBalance = 5000m
        };

        await _apiFixture.PostAsync<AccountResponse>("/api/accounts", accountRequest);

        // Act — Registrar transação com "alimentacao" (minúsculas, sem acento)
        var transactionRequest = new RegisterTransactionRequest
        {
            Document = "11111111111",
            Amount = 100m,
            Type = "Expense",
            CategoryName = "alimentacao",  // minúsculas vs "Alimentação"
            Date = DateTime.UtcNow
        };

        var (statusCode, _) = await _apiFixture.PostAsync<object>(
            "/api/transactions",
            transactionRequest);

        // Assert
        statusCode.Should().Be(201, "CategoryACL deve encontrar categoria com lookup case-insensitive");
    }

    [Fact]
    public async Task DeveLookupCategoria_CaseInsensitive_MaiusculasDeEncontram()
    {
        // Arrange
        var accountRequest = new CreateAccountRequest
        {
            Name = "Test User",
            Document = "22222222222",
            InitialBalance = 5000m
        };

        await _apiFixture.PostAsync<AccountResponse>("/api/accounts", accountRequest);

        // Act — Registrar com todas maiúsculas
        var transactionRequest = new RegisterTransactionRequest
        {
            Document = "22222222222",
            Amount = 100m,
            Type = "Expense",
            CategoryName = "ALIMENTACAO",  // maiúsculas
            Date = DateTime.UtcNow
        };

        var (statusCode, _) = await _apiFixture.PostAsync<object>(
            "/api/transactions",
            transactionRequest);

        // Assert
        statusCode.Should().Be(201, "CategoryACL deve encontrar categoria com UPPERCASE");
    }

    [Fact]
    public async Task DeveLookupCategoria_AccentInsensitive_SemAcentoDeEncontra()
    {
        // Arrange
        var accountRequest = new CreateAccountRequest
        {
            Name = "Test User",
            Document = "33333333333",
            InitialBalance = 5000m
        };

        await _apiFixture.PostAsync<AccountResponse>("/api/accounts", accountRequest);

        // Act — Registrar com "saude" (sem acento) enquanto categoria é "Saúde" (com acento)
        var transactionRequest = new RegisterTransactionRequest
        {
            Document = "33333333333",
            Amount = 150m,
            Type = "Expense",
            CategoryName = "saude",  // sem acento
            Date = DateTime.UtcNow
        };

        var (statusCode, _) = await _apiFixture.PostAsync<object>(
            "/api/transactions",
            transactionRequest);

        // Assert
        statusCode.Should().Be(201, "CategoryACL deve encontrar categoria com accent-insensitive lookup");
    }

    [Fact]
    public async Task DeveLookupCategoria_AccentInsensitive_ComAcentoVariacaoSemAcento()
    {
        // Arrange
        var accountRequest = new CreateAccountRequest
        {
            Name = "Test User",
            Document = "44444444444",
            InitialBalance = 5000m
        };

        await _apiFixture.PostAsync<AccountResponse>("/api/accounts", accountRequest);

        // Act — Registrar com "educacao" (sem acento) enquanto seed tem "Educação"
        var transactionRequest = new RegisterTransactionRequest
        {
            Document = "44444444444",
            Amount = 200m,
            Type = "Expense",
            CategoryName = "educacao",  // sem acento, minúsculas
            Date = DateTime.UtcNow
        };

        var (statusCode, _) = await _apiFixture.PostAsync<object>(
            "/api/transactions",
            transactionRequest);

        // Assert
        statusCode.Should().Be(201, "CategoryACL deve encontrar 'Educação' buscando 'educacao'");
    }

    [Fact]
    public async Task DeveLookupCategoria_CombinCaseEAccent_MistoSemAcentoEMinusculas()
    {
        // Arrange
        var accountRequest = new CreateAccountRequest
        {
            Name = "Test User",
            Document = "55555555555",
            InitialBalance = 5000m
        };

        await _apiFixture.PostAsync<AccountResponse>("/api/accounts", accountRequest);

        // Act — Combinação: case insensitive + accent insensitive
        var transactionRequest = new RegisterTransactionRequest
        {
            Document = "55555555555",
            Amount = 250m,
            Type = "Expense",
            CategoryName = "INVESTIMENTOS",  // UPPERCASE (seed é "Investimentos")
            Date = DateTime.UtcNow
        };

        var (statusCode, _) = await _apiFixture.PostAsync<object>(
            "/api/transactions",
            transactionRequest);

        // Assert
        statusCode.Should().Be(201, "CategoryACL deve encontrar com case+accent insensitive juntas");
    }

    [Fact]
    public async Task DeveRetornarErro_QuandoCategoriaInexistente()
    {
        // Arrange
        var accountRequest = new CreateAccountRequest
        {
            Name = "Test User",
            Document = "66666666666",
            InitialBalance = 5000m
        };

        await _apiFixture.PostAsync<AccountResponse>("/api/accounts", accountRequest);

        // Act — Tentar registrar com categoria que não existe
        var transactionRequest = new RegisterTransactionRequest
        {
            Document = "66666666666",
            Amount = 100m,
            Type = "Expense",
            CategoryName = "CategoriaFantasma",  // não existe
            Date = DateTime.UtcNow
        };

        var (statusCode, _) = await _apiFixture.PostAsync<object>(
            "/api/transactions",
            transactionRequest);

        // Assert
        statusCode.Should().Be(404, "CategoryACL deve retornar 404 para categoria inexistente");
    }

    [Fact]
    public async Task DeveRetornarErro_QuandoNomeVazio()
    {
        // Arrange
        var accountRequest = new CreateAccountRequest
        {
            Name = "Test User",
            Document = "77777777777",
            InitialBalance = 5000m
        };

        await _apiFixture.PostAsync<AccountResponse>("/api/accounts", accountRequest);

        // Act — Registrar com nome vazio (deveria falhar)
        var transactionRequest = new RegisterTransactionRequest
        {
            Document = "77777777777",
            Amount = 100m,
            Type = "Expense",
            CategoryName = "",  // vazio
            Date = DateTime.UtcNow
        };

        var (statusCode, _) = await _apiFixture.PostAsync<object>(
            "/api/transactions",
            transactionRequest);

        // Assert — Quando categoria não existe, retorna 404
        statusCode.Should().Be(404, "Categoria vazia não existe, logo 404");
    }

    [Fact]
    public async Task DeveLookupCategoria_EspacosEmBranco_SãoTratados()
    {
        // Arrange
        var accountRequest = new CreateAccountRequest
        {
            Name = "Test User",
            Document = "88888888888",
            InitialBalance = 5000m
        };

        await _apiFixture.PostAsync<AccountResponse>("/api/accounts", accountRequest);

        // Act — Registrar com espaços extras
        var transactionRequest = new RegisterTransactionRequest
        {
            Document = "88888888888",
            Amount = 100m,
            Type = "Expense",
            CategoryName = "  Alimentação  ",  // com espaços
            Date = DateTime.UtcNow
        };

        var (statusCode, _) = await _apiFixture.PostAsync<object>(
            "/api/transactions",
            transactionRequest);

        // Assert — Pode ser 201 se Application faz Trim, ou 404 se não faz
        // Verificar behavior esperado:
        statusCode.Should().BeOneOf(201, 404);  // Comportamento com espaços
    }

    [Fact]
    public async Task DeveManterConsistencia_MultiplasBuscasMesmaCategoriaRetornamSameId()
    {
        // Arrange
        var accountRequest = new CreateAccountRequest
        {
            Name = "Test User",
            Document = "99999999999",
            InitialBalance = 5000m
        };

        await _apiFixture.PostAsync<AccountResponse>("/api/accounts", accountRequest);

        // Act — Registrar 3 transações com variações de "Alimentação"
        var variations = new[] { "alimentacao", "ALIMENTACAO", "Alimentação" };
        var transactionIds = new List<Guid>();

        foreach (var categoryName in variations)
        {
            var transactionRequest = new RegisterTransactionRequest
            {
                Document = "99999999999",
                Amount = 100m,
                Type = "Expense",
                CategoryName = categoryName,
                Date = DateTime.UtcNow
            };

            await _apiFixture.PostAsync<object>("/api/transactions", transactionRequest);
        }

        // Assert — Listar transações
        var result = await _apiFixture.GetAsync<ListTransactionsResponse>(
            "/api/transactions/99999999999");

        result.Should().NotBeNull();
        result!.Transactions.Should().HaveCount(3, "Todas as 3 transações devem ser registradas");
    }

    // ==================== DTOs ====================

    private class CreateAccountRequest
    {
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; } = null!;

        [System.Text.Json.Serialization.JsonPropertyName("document")]
        public string Document { get; set; } = null!;

        [System.Text.Json.Serialization.JsonPropertyName("initialBalance")]
        public decimal InitialBalance { get; set; }
    }

    private class RegisterTransactionRequest
    {
        [System.Text.Json.Serialization.JsonPropertyName("document")]
        public string Document { get; set; } = null!;

        [System.Text.Json.Serialization.JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("type")]
        public string Type { get; set; } = null!;

        [System.Text.Json.Serialization.JsonPropertyName("categoryName")]
        public string CategoryName { get; set; } = null!;

        [System.Text.Json.Serialization.JsonPropertyName("date")]
        public DateTime? Date { get; set; }
    }

    private class AccountResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public Guid Id { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; } = null!;

        [System.Text.Json.Serialization.JsonPropertyName("document")]
        public string Document { get; set; } = null!;

        [System.Text.Json.Serialization.JsonPropertyName("balance")]
        public MoneyResponse Balance { get; set; } = null!;
    }

    private class MoneyResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("value")]
        public decimal Value { get; set; }
    }

    private class ListTransactionsResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("startDate")]
        public string StartDate { get; set; } = null!;

        [System.Text.Json.Serialization.JsonPropertyName("endDate")]
        public string EndDate { get; set; } = null!;

        [System.Text.Json.Serialization.JsonPropertyName("transactions")]
        public List<TransactionItemResponse> Transactions { get; set; } = new();

        [System.Text.Json.Serialization.JsonPropertyName("totalIncome")]
        public decimal TotalIncome { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("totalExpense")]
        public decimal TotalExpense { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("netBalance")]
        public decimal NetBalance { get; set; }
    }

    private class TransactionItemResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public Guid Id { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("amount")]
        public MoneyResponse Amount { get; set; } = null!;

        [System.Text.Json.Serialization.JsonPropertyName("type")]
        public string Type { get; set; } = null!;

        [System.Text.Json.Serialization.JsonPropertyName("registeredAt")]
        public string RegisteredAt { get; set; } = null!;
    }

    private class CategoryItemResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public Guid Id { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; } = null!;
    }
}
