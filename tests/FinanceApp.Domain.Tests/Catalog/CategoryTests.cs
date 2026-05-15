using FluentAssertions;
using FinanceApp.Catalog.Exceptions;

using CategoryEntity = FinanceApp.Catalog.Category.Category;

namespace FinanceApp.Domain.Tests.Catalog;

public class CategoryTests
{
    private static CategoryEntity CreateCategory(string name = "Alimentação") =>
        new(Guid.NewGuid(), name);

    [Fact]
    public void DeveCriarCategoria_QuandoIdENameSaoValidos()
    {
        // Arrange
        var id = Guid.NewGuid();
        var name = "Transporte";

        // Act
        var category = new CategoryEntity(id, name);

        // Assert
        category.Id.Should().Be(id);
        category.Name.Should().Be(name);
    }

    [Fact]
    public void DeveLancarCategoryException_QuandoIdForVazio()
    {
        // Arrange & Act
        var act = () => new CategoryEntity(Guid.Empty, "Alimentação");

        // Assert
        act.Should().Throw<CategoryException>()
           .WithMessage("*Id da categoria inválido*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void DeveLancarCategoryException_QuandoNomeForNuloOuVazio(string? name)
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var act = () => new CategoryEntity(id, name!);

        // Assert
        act.Should().Throw<CategoryException>()
           .WithMessage("*Nome da categoria é obrigatório*");
    }

    [Fact]
    public void DeveManterIdIgual_QuandoCategoriaForCriada()
    {
        // Arrange
        var id = Guid.NewGuid();
        var category = new CategoryEntity(id, "Saúde");

        // Act & Assert - as propriedades devem ser read-only
        category.Id.Should().Be(id);
        category.Name.Should().Be("Saúde");
    }

    [Fact]
    public void DevePermitirNomesComCaracteresEspeciais()
    {
        // Arrange & Act
        var category = new CategoryEntity(Guid.NewGuid(), "Saúde & Bem-estar");

        // Assert
        category.Name.Should().Be("Saúde & Bem-estar");
    }

    [Fact]
    public void DevePermitirNomesComEspacosMultiplos()
    {
        // Arrange & Act
        var category = new CategoryEntity(Guid.NewGuid(), "Alimentação  e  Bebidas");

        // Assert
        category.Name.Should().Be("Alimentação  e  Bebidas");
    }
}
