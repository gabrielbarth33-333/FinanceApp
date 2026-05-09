using FinanceApp.Catalog.Category;
using FinanceApp.Domain.Account;
using FinanceApp.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

using TransactionEntity = FinanceApp.Domain.Transaction.Transaction;

namespace FinanceApp.Infrastructure.Persistence;

/// <summary> DbContext unificado — contém os dois Bounded Contexts (Financial + Catalog). </summary>
public class FinanceAppDbContext(DbContextOptions<FinanceAppDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<TransactionEntity> Transactions => Set<TransactionEntity>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AccountConfiguration());
        modelBuilder.ApplyConfiguration(new TransactionConfiguration());
        modelBuilder.ApplyConfiguration(new CategoryConfiguration());
    }
}
