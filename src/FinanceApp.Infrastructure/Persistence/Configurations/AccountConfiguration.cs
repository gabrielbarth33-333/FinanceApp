using FinanceApp.Domain.Account;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Persistence.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id");
        builder.Property(a => a.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(a => a.Document).HasColumnName("document").HasMaxLength(20).IsRequired();
        builder.HasIndex(a => a.Document).IsUnique();

        builder.OwnsOne(a => a.Balance, money =>
        {
            money.Property(m => m.Value).HasColumnName("balance").HasPrecision(18, 2).IsRequired();
        });
    }
}
