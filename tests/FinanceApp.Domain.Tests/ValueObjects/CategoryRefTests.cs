using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.ValueObjects;
using FluentAssertions;

namespace FinanceApp.Domain.Tests.ValueObjects;

public class CategoryRefTests
{
    [Fact]
    public void DeveLancarDomainException_QuandoCategoryIdForVazio()
    {
        // Arrange & Act
        var act = () => new CategoryRef(Guid.Empty, "Alimentação");

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("*CategoryId*");
    }

    [Fact]
    public void DeveLancarDomainException_QuandoNomeForNulo()
    {
        // Arrange & Act
        var act = () => new CategoryRef(Guid.NewGuid(), null!);

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("*categoria*");
    }

    [Fact]
    public void DeveLancarDomainException_QuandoNomeForVazio()
    {
        // Arrange & Act
        var act = () => new CategoryRef(Guid.NewGuid(), "   ");

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("*categoria*");
    }

    [Fact]
    public void DeveCriarCategoryRef_QuandoDadosValidos()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var categoryRef = new CategoryRef(id, "Alimentação");

        // Assert
        categoryRef.CategoryId.Should().Be(id);
        categoryRef.Name.Should().Be("Alimentação");
    }
}
