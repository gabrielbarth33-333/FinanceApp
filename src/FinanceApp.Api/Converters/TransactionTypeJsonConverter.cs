using FinanceApp.Domain.Transaction;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FinanceApp.Api.Converters;

// [GRASP: Protected Variations] — Converter customizado que captura erros de desserialização
// de enum e retorna mensagem amigável em vez de mensagem técnica genérica do System.Text.Json.
public class TransactionTypeJsonConverter : JsonConverter<TransactionType>
{
    public override TransactionType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("TransactionType deve ser uma string (Income ou Expense).");

        var value = reader.GetString();
        if (string.IsNullOrWhiteSpace(value))
            throw new JsonException("TransactionType não pode estar vazio.");

        // Case-insensitive para melhor UX
        if (Enum.TryParse<TransactionType>(value, ignoreCase: true, out var result))
            return result;

        var validValues = string.Join(", ", Enum.GetNames(typeof(TransactionType)));
        throw new JsonException($"Valor inválido para TransactionType: '{value}'. Valores aceitos: {validValues}.");
    }

    public override void Write(Utf8JsonWriter writer, TransactionType value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}
