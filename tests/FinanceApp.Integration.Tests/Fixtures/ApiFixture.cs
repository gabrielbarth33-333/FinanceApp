using FinanceApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceApp.Integration.Tests.Fixtures;

/// <summary>
/// [GRASP: Pure Fabrication] — Factory que cria uma instância in-memory da API
/// com DbContext configurado para usar o banco de testes (PostgreSQL via Testcontainers).
/// </summary>
public class ApiFixture : WebApplicationFactory<Program>
{
    private readonly DatabaseFixture _databaseFixture;

    public HttpClient Client { get; private set; } = null!;

    public ApiFixture(DatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove o DbContext padrão
            var descriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<FinanceAppDbContext>));
            
            if (descriptor != null)
                services.Remove(descriptor);

            // Injeta o DbContext configurado para usar a conexão de testes
            services.AddDbContext<FinanceAppDbContext>(opts =>
                opts.UseNpgsql(_databaseFixture.ConnectionString));
        });
    }

    public void Initialize()
    {
        Client = CreateClient();
    }

    /// <summary> Helpers para fazer requisições sem escrita repetida. </summary>
    
    public async Task<T?> GetAsync<T>(string url) where T : class
    {
        var response = await Client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return System.Text.Json.JsonSerializer.Deserialize<T>(json, 
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public async Task<(int StatusCode, T? Data)> PostAsync<T>(string url, object payload) where T : class
    {
        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var response = await Client.PostAsync(url, content);
        var responseJson = await response.Content.ReadAsStringAsync();
        
        try
        {
            var data = System.Text.Json.JsonSerializer.Deserialize<T>(responseJson,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return ((int)response.StatusCode, data);
        }
        catch (System.Text.Json.JsonException ex)
        {
            throw new InvalidOperationException(
                $"Erro desserializando resposta. Status: {response.StatusCode}\nResponse: {responseJson}",
                ex);
        }
    }
}
