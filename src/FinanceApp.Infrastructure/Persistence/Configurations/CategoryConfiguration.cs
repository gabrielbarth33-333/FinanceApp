using FinanceApp.Catalog.Category;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceApp.Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.HasIndex(c => c.Name).IsUnique();

        // [SOLID: OCP] — Seed declarado na configuração; novas categorias = nova migration, sem alterar código existente
        builder.HasData(
            new Category(new Guid("785a055d-c8ac-4903-89ec-285ae98a715b"), "Alimentação"),
            new Category(new Guid("95e52813-82b5-400c-9ba6-44228c754352"), "Transporte"),
            new Category(new Guid("ae1486ca-a148-43c9-873f-3d70222c11d7"), "Moradia"),
            new Category(new Guid("0a4f6705-e9a9-4176-af3b-61c7c82510a6"), "Saúde"),
            new Category(new Guid("3c0a2e0b-a481-4969-aa3c-e634856aff8b"), "Lazer"),
            new Category(new Guid("22e58162-c197-4053-afe7-3f196c1588d7"), "Educação"),
            new Category(new Guid("79c1c1e9-ae9f-425f-84f1-28d5abb13864"), "Salário"),
            new Category(new Guid("40e2f533-d1f6-4bae-9b3b-ebacbef7f0b9"), "Investimentos"),
            new Category(new Guid("c989c51b-1694-421f-b4ce-6f2bdff58688"), "Outros")
        );
    }
}
