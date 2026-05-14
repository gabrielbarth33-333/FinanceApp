using FinanceApp.Integration.Tests.Fixtures;
using System.Net;
using System.Text.Json.Serialization;

namespace FinanceApp.Integration.Tests.Scenarios;

/// <summary>
/// [SOLID: SRP] — Suite de testes de integração para o Agregado Account.
/// Testa fluxos reais: API → Application → Domain → Infrastructure → PostgreSQL.
/// Não usa Mocks — apenas o banco de testes (Testcontainers).
///
/// [GRASP: Controller] — Usa ApiFixture para delegar requisições HTTP e verificar respostas reais.
/// </summary>
public class AccountIntegrationTests : IAsyncLifetime
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
    public async Task DeveCriarConta_QuandoDocumentoValido()
    {
        // Arrange
        var request = new CreateAccountRequest
        {
            Name = "João Silva",
            Document = "12345678900",
            InitialBalance = 5000.00m
        };

        // Act
        var (statusCode, account) = await _apiFixture.PostAsync<AccountResponse>(
            "/api/accounts",
            request);

        // Assert
        statusCode.Should().Be(201);
        account.Should().NotBeNull();
        account!.Name.Should().Be("João Silva");
        account.Document.Should().Be("12345678900");
        account.Balance.Value.Should().Be(5000.00m);
        account.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task DeveBuscarConta_PorId_AposPublicacao()
    {
        // Arrange
        var createRequest = new CreateAccountRequest
        {
            Name = "Maria Santos",
            Document = "98765432100",
            InitialBalance = 3000.00m
        };

        var (_, createdAccount) = await _apiFixture.PostAsync<AccountResponse>(
            "/api/accounts",
            createRequest);

        var accountId = createdAccount!.Id;

        // Act
        var retrievedAccount = await _apiFixture.GetAsync<AccountResponse>(
            $"/api/accounts/{accountId}");

        // Assert
        retrievedAccount.Should().NotBeNull();
        retrievedAccount!.Id.Should().Be(accountId);
        retrievedAccount.Name.Should().Be("Maria Santos");
        retrievedAccount.Document.Should().Be("98765432100");
    }

    [Fact]
    public async Task DeveBuscarConta_PorDocumento_ComMascara_ViaPropriedadeSanitizacao()
    {
        // Arrange
        var createRequest = new CreateAccountRequest
        {
            Name = "Pedro Costa",
            Document = "123.456.789-00", // com máscara
            InitialBalance = 2000.00m
        };

        // Criar conta com máscara
        await _apiFixture.PostAsync<AccountResponse>(
            "/api/accounts",
            createRequest);

        // Act — buscar com máscara diferente da armazenada
        var account = await _apiFixture.GetAsync<AccountResponse>(
            "/api/accounts/document/123.456.789-00");

        // Assert — deve encontrar porque Document sanitiza ambos
        account.Should().NotBeNull();
        account!.Name.Should().Be("Pedro Costa");
        account.Document.Should().Be("12345678900"); // armazenado sem máscara
    }

    [Fact]
    public async Task DeveRetornarErro_AoCriarConta_ComDocumentoDuplicado()
    {
        // Arrange
        var request1 = new CreateAccountRequest
        {
            Name = "Alice",
            Document = "11111111111",
            InitialBalance = 1000.00m
        };

        var request2 = new CreateAccountRequest
        {
            Name = "Bob",
            Document = "11111111111", // documento igual
            InitialBalance = 2000.00m
        };

        // Criar primeira conta
        await _apiFixture.PostAsync<AccountResponse>(
            "/api/accounts",
            request1);

        // Act — tentar criar segunda com mesmo documento
        var (statusCode, _) = await _apiFixture.PostAsync<object>(
            "/api/accounts",
            request2);

        // Assert
        statusCode.Should().Be(404); // AccountException mapeia para 404
    }

    // ==================== DTOs de Resposta ====================

    private class CreateAccountRequest
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;

        [JsonPropertyName("document")]
        public string Document { get; set; } = null!;

        [JsonPropertyName("initialBalance")]
        public decimal InitialBalance { get; set; }
    }

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
}
