using FinanceApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace FinanceApp.Integration.Tests.Fixtures;

/// <summary>
/// [GRASP: Pure Fabrication] — Fixture isolada que gerencia o ciclo de vida do PostgreSQL
/// via Testcontainers. Cada teste recebe seu próprio container e banco de dados limpo.
/// </summary>
public class DatabaseFixture : IAsyncLifetime, IAsyncDisposable
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .Build();

    public string? ConnectionString { get; private set; }
    public DbContextOptions<FinanceAppDbContext> DbContextOptions { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        
        ConnectionString = _container.GetConnectionString();
        
        DbContextOptions = new DbContextOptionsBuilder<FinanceAppDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        // Criar as tabelas e popular categorias padrão
        using (var context = new FinanceAppDbContext(DbContextOptions))
        {
            await context.Database.MigrateAsync();
        }
    }

    public async Task DisposeAsync()
    {
        await _container.StopAsync();
        await _container.DisposeAsync();
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await DisposeAsync();
    }

    /// <summary> Cria um DbContext limpo para cada teste. </summary>
    public FinanceAppDbContext CreateDbContext()
    {
        return new FinanceAppDbContext(DbContextOptions);
    }

    /// <summary> Limpa todas as entidades após teste (simula rollback). </summary>
    public async Task ResetAsync()
    {
        using var context = new FinanceAppDbContext(DbContextOptions);
        
        var accounts = await context.Accounts.ToListAsync();
        var transactions = await context.Transactions.ToListAsync();
        
        context.Transactions.RemoveRange(transactions);
        context.Accounts.RemoveRange(accounts);
        
        await context.SaveChangesAsync();
    }
}
