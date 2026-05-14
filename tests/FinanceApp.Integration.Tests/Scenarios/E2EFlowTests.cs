using FinanceApp.Integration.Tests.Fixtures;
using System.Globalization;
using System.Text.Json.Serialization;

namespace FinanceApp.Integration.Tests.Scenarios;

/// <summary>
/// [SOLID: SRP] — Suite de testes E2E (End-to-End) para fluxos completos.
/// Testa cenários realistas: criar conta → múltiplas transações → gerar relatório
/// com validações complexas de período, balanço, categorias.
///
/// [GRASP: Controller] — Usa ApiFixture para orquestrar requisições HTTP em sequência.
/// </summary>
public class E2EFlowTests : IAsyncLifetime
{
    private readonly DatabaseFixture _databaseFixture = new();
    private ApiFixture _apiFixture = null!;

    public async Task InitializeAsync()
    {
        await _databaseFixture.InitializeAsync();
        await _databaseFixture.ResetAsync();  // Reset antes de cada teste
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
    public async Task DeveCompletarFluxoCompleto_CriarConta_RegistrarTransacoes_ValidarRelatorio()
    {
        // ============= ARRANGE =============
        // 1. Criar conta
        var accountRequest = new CreateAccountRequest
        {
            Name = "João Silva",
            Document = "12345678900",
            InitialBalance = 5000m
        };

        var (accountStatusCode, account) = await _apiFixture.PostAsync<AccountResponse>(
            "/api/accounts",
            accountRequest);

        accountStatusCode.Should().Be(201);
        account.Should().NotBeNull();

        // ============= ACT & ASSERT (TRANSAÇÕES) =============
        // 2. Registrar despesa 1 (Alimentação)
        var expense1 = new RegisterTransactionRequest
        {
            Document = "12345678900",
            Amount = 150m,
            Type = "Expense",
            CategoryName = "Alimentação",
            Date = DateTime.UtcNow.AddDays(-5)
        };

        var (tx1Status, _) = await _apiFixture.PostAsync<object>("/api/transactions", expense1);
        tx1Status.Should().Be(201);

        // 3. Registrar despesa 2 (Transporte)
        var expense2 = new RegisterTransactionRequest
        {
            Document = "12345678900",
            Amount = 80m,
            Type = "Expense",
            CategoryName = "Transporte",
            Date = DateTime.UtcNow.AddDays(-3)
        };

        var (tx2Status, _) = await _apiFixture.PostAsync<object>("/api/transactions", expense2);
        tx2Status.Should().Be(201);

        // 4. Registrar receita (Salário)
        var income = new RegisterTransactionRequest
        {
            Document = "12345678900",
            Amount = 2000m,
            Type = "Income",
            CategoryName = "Salário",
            Date = DateTime.UtcNow.AddDays(-2)
        };

        var (incomeStatus, _) = await _apiFixture.PostAsync<object>("/api/transactions", income);
        incomeStatus.Should().Be(201);

        // ============= VALIDAR RELATÓRIO =============
        // 5. Listar transações e validar
        var result = await _apiFixture.GetAsync<ListTransactionsResponse>(
            "/api/transactions/12345678900");

        result.Should().NotBeNull();
        result!.Transactions.Should().HaveCount(3);
        result.TotalIncome.Should().Be(2000m);
        result.TotalExpense.Should().Be(230m);  // 150 + 80
        result.NetBalance.Should().Be(1770m);   // 2000 - 230
    }

    [Fact]
    public async Task DeveManipularMultiplasCont_Independentemente()
    {
        // Criar 3 contas com transações diferentes
        var documents = new[] { "11111111111", "22222222222", "33333333333" };
        var accounts = new List<(string Document, decimal InitialBalance)>
        {
            ("11111111111", 1000m),
            ("22222222222", 5000m),
            ("33333333333", 10000m)
        };

        // Criar contas
        foreach (var (doc, balance) in accounts)
        {
            var createRequest = new CreateAccountRequest
            {
                Name = $"Cliente {doc}",
                Document = doc,
                InitialBalance = balance
            };

            var (statusCode, _) = await _apiFixture.PostAsync<AccountResponse>(
                "/api/accounts",
                createRequest);

            statusCode.Should().Be(201);
        }

        // Registrar transações diferentes para cada conta
        // Conta 1: 2 despesas
        for (int i = 0; i < 2; i++)
        {
            var tx = new RegisterTransactionRequest
            {
                Document = "11111111111",
                Amount = 100m,
                Type = "Expense",
                CategoryName = "Alimentação",
                Date = DateTime.UtcNow.AddDays(-i)
            };

            var (status, _) = await _apiFixture.PostAsync<object>("/api/transactions", tx);
            status.Should().Be(201);
        }

        // Conta 2: 1 receita + 1 despesa
        for (int i = 0; i < 2; i++)
        {
            var tx = new RegisterTransactionRequest
            {
                Document = "22222222222",
                Amount = 500m,
                Type = i == 0 ? "Income" : "Expense",
                CategoryName = i == 0 ? "Salário" : "Saúde",
                Date = DateTime.UtcNow.AddDays(-i)
            };

            var (status, _) = await _apiFixture.PostAsync<object>("/api/transactions", tx);
            status.Should().Be(201);
        }

        // Conta 3: sem transações

        // Validar que transações não se misturam
        var result1 = await _apiFixture.GetAsync<ListTransactionsResponse>(
            "/api/transactions/11111111111");
        var result2 = await _apiFixture.GetAsync<ListTransactionsResponse>(
            "/api/transactions/22222222222");
        var result3 = await _apiFixture.GetAsync<ListTransactionsResponse>(
            "/api/transactions/33333333333");

        // Conta 1: 2 despesas
        result1!.Transactions.Should().HaveCount(2);
        result1.TotalExpense.Should().Be(200m);
        result1.TotalIncome.Should().Be(0m);

        // Conta 2: 1 receita + 1 despesa
        result2!.Transactions.Should().HaveCount(2);
        result2.TotalIncome.Should().Be(500m);
        result2.TotalExpense.Should().Be(500m);
        result2.NetBalance.Should().Be(0m);

        // Conta 3: sem transações
        result3!.Transactions.Should().HaveCount(0);
        result3.TotalIncome.Should().Be(0m);
        result3.TotalExpense.Should().Be(0m);
    }

    [Fact]
    public async Task DeveFiltraTransacoes_PorPeriodo_Corretamente()
    {
        // Criar conta
        var accountRequest = new CreateAccountRequest
        {
            Name = "Alice",
            Document = "44444444444",
            InitialBalance = 10000m
        };

        await _apiFixture.PostAsync<AccountResponse>("/api/accounts", accountRequest);

        // Registrar transações em períodos diferentes
        var dates = new[]
        {
            DateTime.UtcNow.AddDays(-60),  // 2 meses atrás
            DateTime.UtcNow.AddDays(-30),  // 1 mês atrás
            DateTime.UtcNow.AddDays(-15),  // 15 dias atrás
            DateTime.UtcNow.AddDays(-5),   // 5 dias atrás
            DateTime.UtcNow                 // hoje
        };

        foreach (var (i, date) in dates.Select((d, idx) => (idx, d)))
        {
            var tx = new RegisterTransactionRequest
            {
                Document = "44444444444",
                Amount = (i + 1) * 100m,
                Type = "Expense",
                CategoryName = "Alimentação",
                Date = date
            };

            await _apiFixture.PostAsync<object>("/api/transactions", tx);
        }

        // Filtro 1: Últimos 30 dias (deve retornar 4 transações: 200, 300, 400, 500)
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        var result30Days = await _apiFixture.GetAsync<ListTransactionsResponse>(
            $"/api/transactions/44444444444?startDate={thirtyDaysAgo}&endDate={today}");

        result30Days!.Transactions.Should().HaveCount(4);  // 200 + 300 + 400 + 500
        result30Days.TotalExpense.Should().Be(1400m);      // 200 + 300 + 400 + 500

        // Filtro 2: Últimos 10 dias (deve retornar 2 transações: 400, 500)
        var tenDaysAgo = DateTime.UtcNow.AddDays(-10).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        var result10Days = await _apiFixture.GetAsync<ListTransactionsResponse>(
            $"/api/transactions/44444444444?startDate={tenDaysAgo}&endDate={today}");

        result10Days!.Transactions.Should().HaveCount(2);  // 400 + 500
        result10Days.TotalExpense.Should().Be(900m);       // 400 + 500
    }

    [Fact]
    public async Task DeveHandleTransacoesMistas_IncomesEExpenses_ComBaralhoTotal()
    {
        // Criar conta
        var accountRequest = new CreateAccountRequest
        {
            Name = "Bob",
            Document = "55555555555",
            InitialBalance = 5000m
        };

        var (createStatus, createdAccount) = await _apiFixture.PostAsync<AccountResponse>("/api/accounts", accountRequest);
        createStatus.Should().Be(201);
        createdAccount.Should().NotBeNull();

        // Simular fluxo realista de mês
        var transactions = new[]
        {
            (100m, "Expense", "Alimentação"),
            (50m, "Expense", "Transporte"),
            (1500m, "Income", "Salário"),
            (200m, "Expense", "Saúde"),
            (300m, "Expense", "Lazer"),
            (75m, "Expense", "Educação"),
            (500m, "Income", "Investimentos"),  // Freelance → Investimentos
            (100m, "Expense", "Moradia"),
            (25m, "Expense", "Outros"),
        };

        foreach (var (amount, type, category) in transactions)
        {
            var tx = new RegisterTransactionRequest
            {
                Document = "55555555555",
                Amount = amount,
                Type = type,
                CategoryName = category,
                Date = DateTime.UtcNow
            };

            var (status, _) = await _apiFixture.PostAsync<object>("/api/transactions", tx);
            status.Should().Be(201, $"Falha ao registrar transação: {amount} - {type} - {category}");
        }

        // Validar relatório
        var result = await _apiFixture.GetAsync<ListTransactionsResponse>(
            "/api/transactions/55555555555");

        result.Should().NotBeNull();
        result!.Transactions.Should().HaveCount(9);
        
        // Total de receitas
        result.TotalIncome.Should().Be(2000m);  // 1500 + 500

        // Total de despesas
        result.TotalExpense.Should().Be(850m);  // 100 + 50 + 200 + 300 + 75 + 100 + 25

        // Balanço líquido
        result.NetBalance.Should().Be(1150m);  // 2000 - 850
    }

    [Fact]
    public async Task DeveTratarCategoriasDiversas_ComSanitizacaoAcento_Case()
    {
        // Criar conta
        var accountRequest = new CreateAccountRequest
        {
            Name = "Charlie",
            Document = "66666666666",
            InitialBalance = 5000m
        };

        await _apiFixture.PostAsync<AccountResponse>("/api/accounts", accountRequest);

        // Registrar transações com categorias em formatos diferentes
        var categoryVariations = new[]
        {
            ("Alimentação", "alimentacao"),    // acento
            ("Saúde", "SAUDE"),                 // uppercase, sem acento
            ("Educação", "EDUCACAO"),           // acento + uppercase
            ("Moradia", "moradia"),             // lowercase
            ("Lazer", "LAZER"),                 // uppercase
            ("Transporte", "transporte"),       // lowercase
        };

        decimal totalExpected = 0;

        foreach (var (i, (canonical, variant)) in categoryVariations.Select((x, idx) => (idx, x)))
        {
            var tx = new RegisterTransactionRequest
            {
                Document = "66666666666",
                Amount = (i + 1) * 50m,  // 50, 100, 150, 200, 250, 300
                Type = "Expense",
                CategoryName = variant,  // usar variação
                Date = DateTime.UtcNow
            };

            var (status, _) = await _apiFixture.PostAsync<object>("/api/transactions", tx);
            status.Should().Be(201);
            totalExpected += (i + 1) * 50m;
        }

        // Validar que todas as transações foram registradas
        var result = await _apiFixture.GetAsync<ListTransactionsResponse>(
            "/api/transactions/66666666666");

        result!.Transactions.Should().HaveCount(6);
        result.TotalExpense.Should().Be(totalExpected);  // 50+100+150+200+250+300 = 1050
    }

    [Fact]
    public async Task DeveValidarSeguracaIsolamento_DocumentosSanitizados()
    {
        // Criar duas contas: uma com máscara, outra sem
        var account1 = new CreateAccountRequest
        {
            Name = "Account 1",
            Document = "12345678901",  // sem máscara
            InitialBalance = 1000m
        };

        var account2 = new CreateAccountRequest
        {
            Name = "Account 2",
            Document = "123.456.789-01",  // com máscara (mesmo documento)
            InitialBalance = 2000m
        };

        var (status1, _) = await _apiFixture.PostAsync<AccountResponse>(
            "/api/accounts",
            account1);

        // Tentar criar segunda com máscara (deve falhar — documento duplicado)
        var (status2, _) = await _apiFixture.PostAsync<object>(
            "/api/accounts",
            account2);

        status1.Should().Be(201);
        status2.Should().Be(404);  // Documento já existe (sanitizado)

        // Registrar transação com máscara
        var tx = new RegisterTransactionRequest
        {
            Document = "123.456.789-01",  // com máscara
            Amount = 100m,
            Type = "Expense",
            CategoryName = "Alimentação",
            Date = DateTime.UtcNow
        };

        var (txStatus, _) = await _apiFixture.PostAsync<object>("/api/transactions", tx);
        txStatus.Should().Be(201);  // Deve encontrar conta (sanitização)

        // Buscar transações usando documento sem máscara
        var result = await _apiFixture.GetAsync<ListTransactionsResponse>(
            "/api/transactions/12345678901");  // sem máscara

        result!.Transactions.Should().HaveCount(1);  // Achou a transação
    }

    // ==================== DTOs ====================

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

        [JsonPropertyName("amount")]
        public MoneyResponse Amount { get; set; } = null!;

        [JsonPropertyName("type")]
        public string Type { get; set; } = null!;

        [JsonPropertyName("registeredAt")]
        public string RegisteredAt { get; set; } = null!;
    }
}
