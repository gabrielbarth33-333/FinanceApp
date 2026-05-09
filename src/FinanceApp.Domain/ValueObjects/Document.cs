using FinanceApp.Domain.Exceptions;
using System.Text.RegularExpressions;

namespace FinanceApp.Domain.ValueObjects;

// [SOLID: SRP] — Document tem uma única responsabilidade: representar e validar um documento (CPF/CNPJ),
// sanitizando automaticamente máscaras na construção.
//
// [SOLID: DIP] — Account depende desta abstração (value object) em vez de string primitiva,
// garantindo que todo documento é sempre sanitizado.
//
// [GRASP: Information Expert] — Document é o especialista em validar e sanitizar documento,
// não Account ou UseCase.
/// <summary> Value Object imutável que representa um documento (CPF/CNPJ) sanitizado. </summary>
public record Document
{
    /// <summary> Valor sanitizado do documento (apenas alfanuméricos). </summary>
    public string Value { get; }

    /// <summary> Cria um novo Document, sanitizando automaticamente máscaras (remove tudo que não é alfanumérico). </summary>
    public Document(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new DomainException("Documento não pode estar vazio.");

        var cleaned = Regex.Replace(raw, @"[^\w]", "");

        if (string.IsNullOrWhiteSpace(cleaned))
            throw new DomainException("Documento inválido — deve conter pelo menos um caractere alfanumérico.");

        Value = cleaned;
    }

    /// <summary> Retorna a representação em string do documento. </summary>
    public override string ToString() => Value;
}
