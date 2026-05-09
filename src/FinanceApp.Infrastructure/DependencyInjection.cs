using FinanceApp.Application.Account;
using FinanceApp.Application.Interfaces;
using FinanceApp.Application.Transaction;
using FinanceApp.Catalog.Category;
using FinanceApp.Domain.Account;
using FinanceApp.Domain.ACL;
using FinanceApp.Domain.Transaction;
using FinanceApp.Infrastructure.ACL;
using FinanceApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' não configurada.");

        services.AddDbContext<FinanceAppDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Repositórios
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // ACL
        services.AddScoped<ICategoryACL, CategoryACL>();

        // Domain Service
        services.AddScoped<ITransactionDomainService, TransactionDomainService>();

        // Use Cases
        services.AddScoped<RegisterTransactionUseCase>();
        services.AddScoped<ListTransactionsUseCase>();
        services.AddScoped<CreateAccountUseCase>();
        services.AddScoped<GetAccountUseCase>();
        services.AddScoped<GetAccountByDocumentUseCase>();

        return services;
    }
}
