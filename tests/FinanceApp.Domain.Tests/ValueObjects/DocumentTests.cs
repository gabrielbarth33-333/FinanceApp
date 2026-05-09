using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.ValueObjects;
using FluentAssertions;

namespace FinanceApp.Domain.Tests.ValueObjects;

public class DocumentTests
{
    [Fact]
    public void DeveRemoverMascara_CPF()
    {
        // Arrange & Act
        var doc = new Document("123.456.789-00");

        // Assert
        doc.Value.Should().Be("12345678900");
    }

    [Fact]
    public void DeveRemoverMascara_CNPJ()
    {
        // Arrange & Act
        var doc = new Document("12.345.678/0001-90");

        // Assert
        doc.Value.Should().Be("12345678000190");
    }

    [Fact]
    public void DeveRemoverTodosCaracteresEspeciais()
    {
        // Arrange & Act
        var doc = new Document("123-456@789#00");

        // Assert
        doc.Value.Should().Be("12345678900");
    }

    [Fact]
    public void DeveMantermAlfanumericos()
    {
        // Arrange & Act
        var doc = new Document("ABC123XYZ456");

        // Assert
        doc.Value.Should().Be("ABC123XYZ456");
    }

    [Fact]
    public void DeveAceitarDocumentoJaSemMascara()
    {
        // Arrange & Act
        var doc = new Document("12345678900");

        // Assert
        doc.Value.Should().Be("12345678900");
    }

    [Fact]
    public void DeveLancarException_QuandoDocumentoVazio()
    {
        // Arrange & Act
        var act = () => new Document("");

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*vazio*");
    }

    [Fact]
    public void DeveLancarException_QuandoDocumentoForSomenteCaracteresEspeciais()
    {
        // Arrange & Act
        var act = () => new Document("@#$%^&*()");

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*inválido*");
    }

    [Fact]
    public void DeveLancarException_QuandoDocumentoForWhitespace()
    {
        // Arrange & Act
        var act = () => new Document("   ");

        // Assert
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void DeveSerValorObject_Imutavel()
    {
        // Arrange & Act
        var doc1 = new Document("123.456.789-00");
        var doc2 = new Document("123.456.789-00");
        var doc3 = new Document("12345678900");

        // Assert
        doc1.Should().Be(doc2); // mesmo conteúdo = iguais
        doc1.Should().Be(doc3); // mesmo após sanitização
    }
}
