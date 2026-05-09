using FinanceApp.Domain.Transaction;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TransactionEntity = FinanceApp.Domain.Transaction.Transaction;

namespace FinanceApp.Infrastructure.Persistence.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<TransactionEntity>
{
    public void Configure(EntityTypeBuilder<TransactionEntity> builder)
    {
        builder.ToTable("transactions");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");
        builder.Property(t => t.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(t => t.Date).HasColumnName("date").IsRequired();
        builder.Property(t => t.Type).HasColumnName("type")
            .HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.OwnsOne(t => t.Amount, money =>
        {
            money.Property(m => m.Value).HasColumnName("amount").HasPrecision(18, 2).IsRequired();
        });

        builder.OwnsOne(t => t.CategoryRef, cat =>
        {
            cat.Property(c => c.CategoryId).HasColumnName("category_id").IsRequired();
            cat.Property(c => c.Name).HasColumnName("category_name").HasMaxLength(100).IsRequired();
        });
    }
}
