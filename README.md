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

## Endpoints

### Contas

| Método | Rota | Descrição |
|--------|------|-----------|
| `POST` | `/api/accounts` | Cria uma nova conta |
| `GET` | `/api/accounts/{id}` | Busca conta por ID |
| `GET` | `/api/accounts/document/{document}` | Busca conta por CPF/CNPJ |

### Transações

| Método | Rota | Descrição |
|--------|------|-----------|
| `POST` | `/api/transactions` | Registra uma transação |
| `GET` | `/api/transactions/{document}` | Lista transações por documento com balanço do período |

#### Query params — GET `/api/transactions/{document}`

| Parâmetro | Tipo | Descrição |
|-----------|------|-----------|
| `startDate` | `date` | Início do período (padrão: 1 mês atrás) |
| `endDate` | `date` | Fim do período (padrão: hoje) |

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

- **30 testes** — 18 de domínio + 12 de aplicação
- Nomenclatura em português: `Deve[Resultado]_Quando[Condição]`
- Framework: xUnit + Moq + FluentAssertions

---

## Princípios aplicados

O código contém comentários inline destacando onde cada princípio foi aplicado:

- **SOLID**: SRP, OCP, LSP, ISP, DIP
- **GRASP**: Expert, Creator, Controller, Pure Fabrication, Low Coupling, High Cohesion, Protected Variations
