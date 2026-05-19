# FinanceApp

Sistema de controle financeiro pessoal desenvolvido com **C# 13 / .NET 10**, seguindo os princípios de **DDD**, **SOLID** e **GRASP**.

---

## Arquitetura

O projeto é dividido em dois **Bounded Contexts**:

- **Financial Context** — contas e transações
- **Catalog Context** — categorias (referência, não modificável pela aplicação)

### Estrutura de projetos

```
src/
├── FinanceApp.Api              # Controllers, Middlewares, Program.cs
├── FinanceApp.Application      # Use Cases, interfaces de repositório
├── FinanceApp.Domain           # Entidades, Value Objects, Domain Services
├── FinanceApp.Infrastructure   # EF Core, repositórios, ACL
└── FinanceApp.Catalog          # Bounded Context de categorias

tests/
├── FinanceApp.Domain.Tests         # Testes unitários do domínio
├── FinanceApp.Application.Tests    # Testes unitários dos use cases
└── FinanceApp.Infrastructure.Tests # Testes de integração (Testcontainers)
```

### Dependências entre camadas

```
Api → Application → Domain
Infrastructure → Domain (implementa interfaces)
CategoryACL → Catalog (única ponte entre contextos)
```

> `FinanceApp.Domain` possui **zero dependências externas**.  
> `CategoryACL` é a **única classe** autorizada a referenciar `ICategoryRepository` do Catalog Context.

---

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

---

## Como rodar localmente

### 1. Clone o repositório

```bash
git clone https://github.com/gabrielbarth33-333/FinanceApp.git
cd FinanceApp
```

### 2. Configure as credenciais de desenvolvimento

Crie o arquivo `src/FinanceApp.Api/appsettings.Development.json` (não versionado):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=financeapp;Username=postgres;Password=postgres"
  }
}
```

### 3. Suba o banco de dados

```bash
docker compose up -d
```

### 4. Aplique as migrations

```bash
dotnet ef database update --project src/FinanceApp.Infrastructure --startup-project src/FinanceApp.Api
```

> As migrations criam as tabelas e inserem automaticamente as **9 categorias padrão** (Alimentação, Transporte, Moradia, Saúde, Lazer, Educação, Salário, Investimentos, Outros).

### 5. Rode a API

```bash
dotnet run --project src/FinanceApp.Api
```

A API estará disponível em `https://localhost:5001` com documentação OpenAPI em `/openapi/v1.json`.

---

## Funcionalidades principais

### Sanitização de documentos (CPF/CNPJ)

- **Document value object** imutável que sanitiza automaticamente máscaras na construção
- Remove tudo que não é alfanumérico: `"123.456.789-00"` → `"12345678900"`
- Validação em nível de domínio via `DomainException`
- Aplicado em todos os use cases: criar conta, buscar conta, registrar transação, listar transações

**Exemplos:**
```
Entrada: "123.456.789-00" → Armazenado: "12345678900"
Entrada: "12.345.678/0001-90" → Armazenado: "12345678000190"
Busca por "123.456.789-00" encontra documento armazenado como "12345678900" ✓
```

### Validação de período de transações

- **Períodos aninhados** — data de início nunca pode ser posterior a data de fim
- **Limite de 1 ano** — períodos maiores que 365 dias são rejeitados
- Ambas as regras lançam exceções de domínio específicas (não `ArgumentException`)
  - `InvalidTransactionPeriodException` — datas inválidas
  - `TransactionPeriodTooLargeException` — período excede 1 ano
- Validação ocorre no `ListTransactionsUseCase` antes de consultar o repositório

**Exemplos:**
```
Período: 2024-03-10 a 2024-02-10 → InvalidTransactionPeriodException ✓
Período: 2023-01-01 a 2024-02-01 → TransactionPeriodTooLargeException ✓
Período: 2024-01-01 a 2024-06-01 → Aceito (180 dias) ✓
```

### Busca de categorias — case e diacrítico-insensível

- PostgreSQL `unaccent()` + `LOWER()` para ignorar case e acentuação
- Exemplo: `"alimentacao"`, `"ALIMENTAÇÃO"`, `"Alimentação"` → todos encontram categoria `"Alimentação"`
- Query segura com `FromSqlInterpolated` (previne SQL injection)

**Exemplos:**
```
Entrada: "alimentacao" → Encontra: "Alimentação" ✓
Entrada: "OUTROS" → Encontra: "Outros" ✓
Entrada: "Saúde" → Encontra: "Saúde" ✓
```

---

## Endpoints

### Contas

| Método | Rota | Descrição |
|--------|------|-----------|
| `POST` | `/api/accounts` | Cria uma nova conta (aceita documento com máscara) |
| `GET` | `/api/accounts/{id}` | Busca conta por ID |
| `GET` | `/api/accounts/document/{document}` | Busca conta por CPF/CNPJ (aceita com máscara) |

**Exemplo POST `/api/accounts`:**
```json
{
  "name": "João Silva",
  "document": "123.456.789-00",
  "initialBalance": 5000.00
}
```

### Transações

| Método | Rota | Descrição |
|--------|------|-----------|
| `POST` | `/api/transactions` | Registra uma transação (aceita categoria com case/acento diferente) |
| `GET` | `/api/transactions/{document}` | Lista transações por documento com balanço do período |

**Exemplo POST `/api/transactions`:**
```json
{
  "document": "123.456.789-00",
  "amount": 150.00,
  "type": "Expense",
  "categoryName": "alimentacao"
}
```

#### Query params — GET `/api/transactions/{document}`

| Parâmetro | Tipo | Descrição |
|-----------|------|-----------|
| `startDate` | `date` | Início do período (padrão: 1 mês atrás) |
| `endDate` | `date` | Fim do período (padrão: hoje) |

**Validações:**
- Data de início **não pode ser posterior** a data de fim (lança `InvalidTransactionPeriodException`)
- Período máximo permitido é de **1 ano** (lança `TransactionPeriodTooLargeException`)

#### Resposta de transações

```json
{
  "startDate": "2026-04-09",
  "endDate": "2026-05-09",
  "transactions": [...],
  "totalIncome": 5000.00,
  "totalExpense": 1200.00,
  "netBalance": 3800.00
}
```

---

## Testes

```bash
dotnet test
```

- **49 testes** — 30 de domínio + 19 de aplicação
- Cobertura: **87.8%** (Domain: 94.6%, Application: 94.7%)
- Nomenclatura em português: `Deve[Resultado]_Quando[Condição]`
- Framework: xUnit + Moq + FluentAssertions
- Testes para sanitização de documento, busca case-insensitiva de categorias e validação de período incluídos

---

## Princípios aplicados

O código contém comentários inline destacando onde cada princípio foi aplicado:

- **SOLID**: SRP, OCP, LSP, ISP, DIP
- **GRASP**: Expert, Creator, Controller, Pure Fabrication, Low Coupling, High Cohesion, Protected Variations
