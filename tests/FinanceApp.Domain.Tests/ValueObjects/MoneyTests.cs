using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.ValueObjects;
using FluentAssertions;

namespace FinanceApp.Domain.Tests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void DeveLancarDomainException_QuandoValorForNegativo()
    {
        // Arrange & Act
        var act = () => new Money(-50m);

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("*positivo*");
    }

    [Fact]
    public void DeveLancarDomainException_QuandoValorForZero()
    {
        // Arrange & Act
        var act = () => new Money(0m);

        // Assert
        act.Should().Throw<DomainException>()
           .WithMessage("*positivo*");
    }

    [Fact]
    public void DeveArredondarParaDuasCasasDecimais_QuandoCriado()
    {
        // Arrange & Act
        var money = new Money(10.999m);

        // Assert
        money.Value.Should().Be(11.00m);
    }

    [Fact]
    public void DeveCriarMoney_QuandoValorPositivoValido()
    {
        // Arrange & Act
        var money = new Money(100.50m);

        // Assert
        money.Value.Should().Be(100.50m);
    }
}
