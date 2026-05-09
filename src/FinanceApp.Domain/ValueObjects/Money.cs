using FinanceApp.Domain.Exceptions;

namespace FinanceApp.Domain.ValueObjects;

// [SOLID: SRP] — Money tem uma única responsabilidade: representar e validar um valor monetário.
// Não sabe nada sobre transações, contas ou persistência.
//
// [SOLID: OCP] — Sendo um record imutável, Money está fechado para modificação (nenhum setter).
// Extensões de comportamento monetário podem ser adicionadas sem alterar este tipo.
/// <summary> <<ValueObject>> Representa um valor monetário sempre positivo com 2 casas decimais. </summary>
public record Money
{
    public decimal Value { get; }

    public Money(decimal value)
    {
        if (value <= 0)
            throw new DomainException("O valor deve ser positivo.");

        // [GRASP: Expert] — Money conhece sua própria regra de arredondamento.
        // Quem melhor sabe como normalizar um valor monetário senão o próprio tipo que o representa?
        Value = Math.Round(value, 2);
    }
}
