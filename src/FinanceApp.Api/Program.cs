using FinanceApp.Api.Converters;
using FinanceApp.Api.Middlewares;
using FinanceApp.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // [SOLID: OCP] — Converter customizado para TransactionType com mensagem clara de erro
        options.JsonSerializerOptions.Converters.Add(new TransactionTypeJsonConverter());
        // [SOLID: OCP] — Converter genérico para outros enums
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// [GRASP: Protected Variations] — Substitui a resposta padrão de validação do ASP.NET Core
// para erros de enum, informando ao usuário os valores aceitos em vez de uma mensagem técnica.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .SelectMany(e => e.Value!.Errors.Select(err =>
            {
                // Detecta erro de conversão de enum e enriquece a mensagem com os valores válidos
                if (err.ErrorMessage.Contains("could not be converted") && e.Key.StartsWith("$."))
                {
                    var fieldName = e.Key.TrimStart('$', '.');
                    var paramType = context.ActionDescriptor.Parameters
                        .FirstOrDefault(p => string.Equals(p.Name, fieldName, StringComparison.OrdinalIgnoreCase))
                        ?.ParameterType;

                    if (paramType?.IsEnum == true)
                    {
                        var valid = string.Join(", ", Enum.GetNames(paramType));
                        return new { field = fieldName, message = $"Valor inválido. Valores aceitos: {valid}." };
                    }
                }

                return new { field = e.Key, message = err.ErrorMessage };
            }))
            .ToList();

        return new BadRequestObjectResult(new { errors });
    };
});

builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.MapControllers();
app.Run();

