# FinanceApp

Sistema de controle financeiro pessoal desenvolvido com **C# 13 / .NET 10**, seguindo os princípios de **DDD**, **SOLID** e **GRASP**.

---

## Escopo do projeto

### Gestão de contas
 - Criação de contas financeiras
 - Consulta de saldo

### Gestão de transações
 - Registro de receitas e despesas
 - Associação com categorias
 - Atualização automática do saldo

### Gestão de categorias
 - Listagem de categorias
 - Classificação por nome

### Regras de negócio
 - Não permitir transações com valor negativo
 - Atualizar saldo da conta a cada transação
 - Impedir saldo negativo
 - Garantir consistência entre tipo da transação e categoria


## Fora do escopo
 - Interface gráfica complexa (o sistema será exposto via API)
 - Integração com sistemas bancários reais
 - Autenticação e autorização de usuários
 - Criação de novas categorias de transação

---

## Modelo do domínio

<img width="1962" height="1031" alt="Image" src="https://github.com/user-attachments/assets/36f3af2d-0026-4ad9-b435-77b8d840d807" />

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


## Rubricas de avaliação
### 1. Aplicar os conceitos de Orientação a Objetos com C#
#### 1.1 O aluno implementou as classes aplicando os conceitos básicos de OO como Encapsulamento, Abstração, Herança e Polimorfismo?	

**Exemplos no projeto:**
- `Money` (`src/FinanceApp.Domain/ValueObjects/Money.cs`)
- `Document` (`src/FinanceApp.Domain/ValueObjects/Document.cs`) 
- `ITransactionDomainService` / `TransactionDomainService` — abstração via interface;
- `ICategoryACL` / `CategoryACL` — abstração do Anti-Corruption Layer;

#### 1.2 O aluno implementou as classes e objetos em C#, aplicando corretamente modificadores de acesso, propriedades, métodos e construtores?	

**Exemplos no projeto:**
- `Account` (`src/FinanceApp.Domain/Account/Account.cs`)
- `Transaction` (`src/FinanceApp.Domain/Transaction/Transaction.cs`) — construtor valida todos os invariantes do agregado; propriedades somente leitura.
- `CreateAccountRequest` (`src/FinanceApp.Api/Controllers/AccountCommandController.cs`) — `record` com propriedade com valor padrão (`decimal InitialBalance = 0.01m`), demonstrando uso correto de construtores posicionais.

#### 1.3 O aluno aplicou herança e polimorfismo em C# para criar hierarquias de classes flexíveis e extensíveis?	

**Exemplos no projeto:**
- `IAccountRepository` / `AccountRepository` — polimorfismo via interface; `RegisterTransactionUseCase` depende de `IAccountRepository`, sem conhecer a implementação EF Core.
- `ITransactionDomainService` / `TransactionDomainService` — substituição polimórfica usada nos testes com `Mock<ITransactionDomainService>` (Moq)
- `ICategoryACL` / `CategoryACL` — hierarquia de interface→implementação

#### 1.4 O aluno aplicou abstração e encapsulamento em C# para ocultar detalhes de implementação e expor interfaces claras e concisas?	

**Exemplos no projeto:**
- `Money` — encapsula regra de arredondamento e rejeição de valores negativos; expõe apenas `Value`.
- `Document` — encapsula sanitização de CPF/CNPJ; consumidores nunca lidam com a string bruta após a construção.
- `IUnitOfWork` (`src/FinanceApp.Application/Interfaces/IUnitOfWork.cs`) — abstrai o controle transacional do EF Core; Use Cases usam `BeginAsync/CommitAsync/RollbackAsync` sem saber nada de `DbContext`.
- `ExceptionHandlingMiddleware` (`src/FinanceApp.Api/Middlewares/ExceptionHandlingMiddleware.cs`) — encapsula o mapeamento de exceções de domínio para respostas HTTP, ocultando esse detalhe dos controllers.

### 2. Modelar aplicações utilizando Domain-Driven Design
#### 2.1 O aluno modelou o domínio utilizando Ubiquitous Language, Entities, Value Objects e Repositories de forma coerente com os conceitos de DDD?	

**Exemplos no projeto:**
- **Entities:** `Account` e `Transaction`
- **Value Objects:** `Money`, `Document`, `CategoryRef` (`src/FinanceApp.Domain/ValueObjects/`)
- **Repositories (interfaces no domínio):** `IAccountRepository`, `ITransactionRepository` — contratos definidos em `FinanceApp.Domain`, implementados em `FinanceApp.Infrastructure` (inversão de dependência).

#### 2.2 O aluno modelou o domínio utilizando Aggregate, Bounded Contexts e Domain Services de maneira estruturada e adequada ao problema?	

**Exemplos no projeto:**
- **Aggregates:** `Account` é a raiz do agregado de contas; `Transaction` é raiz do agregado de movimentações — cada um com seu próprio repositório.
- **Bounded Contexts:** `FinanceApp.Domain` (contexto Finance) e `FinanceApp.Catalog` (contexto Catalog) são projetos separados, cada um com seus próprios modelos de `Category`.
- **Domain Service:** `TransactionDomainService` (`src/FinanceApp.Domain/Transaction/TransactionDomainService.cs`) — orquestra regra de negócio cross-aggregate (validação de saldo) que não pertence a nenhuma entidade isolada.

#### 2.3 O aluno diferenciou claramente Domain Services e Factories na modelagem do domínio, justificando sua escolha com base na responsabilidade de cada elemento?	

**Exemplos no projeto:**
- **Domain Service:** `TransactionDomainService` — responsabilidade de validar regra de negócio que atravessa o agregado `Account` (verificar saldo antes de registrar transação).
- **Factory implícita nos Use Cases:** `CreateAccountUseCase.ExecuteAsync` age como factory de `Account` — centraliza a criação do agregado com todas as validações (`Document`, `Money`, unicidade).

#### 2.4 O aluno modelou o domínio considerando a integração entre Bounded Contexts, aplicando padrões como Anti-Corruption Layer e Context Map com clareza e eficácia?	

**Exemplos no projeto:**
- **Anti-Corruption Layer:** `ICategoryACL` (`src/FinanceApp.Domain/ACL/ICategoryACL.cs`) — interface no domínio Finance que isola qualquer conhecimento do modelo Catalog.
- **Implementação da ACL:** `CategoryACL` (`src/FinanceApp.Infrastructure/ACL/CategoryACL.cs`) — traduz `Category` (modelo Catalog) para `CategoryRef` (Value Object do domínio Finance), impedindo que o modelo externo vaze para o domínio.
- **Context Map:** `FinanceApp.Catalog` é o contexto upstream (fornecedor de categorias); `FinanceApp.Domain` é downstream, protegido pela ACL. A dependência flui apenas via `ICategoryACL`, nunca diretamente via `ICategoryRepository`.

### 3. Criar aplicações empregando padrões de projeto - SOLID e GRASP
#### 3.1 O aluno aplicou os princípios SOLID no design das classes, garantindo coesão, alta responsabilidade e baixo acoplamento?	

**Exemplos no projeto:**
- **SRP:** `AccountCommandController` só lida com criação de conta; `AccountQueryController` só lida com leitura — separação de comandos e consultas.
- **OCP:** Novos Use Cases podem ser adicionados sem modificar os existentes; os controllers dependem de abstrações injetadas.
- **LSP:** `AccountRepository` substitui `IAccountRepository` sem quebrar nenhum consumidor.
- **ISP:** `IAccountRepository`, `ITransactionRepository` e `ICategoryRepository` são interfaces segmentadas por responsabilidade — nenhum cliente é forçado a depender de métodos que não usa.
- **DIP:** `RegisterTransactionUseCase` depende de `IAccountRepository`, `ICategoryACL`, `IUnitOfWork` (abstrações), nunca das implementações concretas de Infrastructure.

#### 3.2 O aluno utilizou corretamente o princípio de Single Responsibility nas classes, evitando a concentração excessiva de responsabilidades?	

**Exemplos no projeto:**
- `CreateAccountUseCase` — única responsabilidade: criar conta. Não persiste, não valida saldo, não trata HTTP.
- `TransactionDomainService` — única responsabilidade: validar saldo antes de debitar.
- `UnitOfWork` (`src/FinanceApp.Infrastructure/Persistence/UnitOfWork.cs`) — única responsabilidade: controlar transações de banco de dados.
- `ExceptionHandlingMiddleware` — única responsabilidade: mapear exceções de domínio para respostas HTTP, sem lógica de negócio.

#### 3.3 O aluno aplicou o padrão Low Coupling para garantir a independência entre as classes e promover a reutilização de código?	

**Exemplos no projeto:**
- `FinanceApp.Domain` — zero dependências externas (sem referência a EF Core, ASP.NET ou qualquer lib); máximo isolamento.
- `RegisterTransactionUseCase` — recebe 5 interfaces via construtor (injeção de dependência); não instancia nenhuma classe concreta, permitindo trocar qualquer implementação sem recompilar o Use Case.
- `CategoryACL` — único ponto de acoplamento entre os contextos Finance e Catalog; o restante do domínio Finance nunca conhece `ICategoryRepository`.

#### 3.4 O aluno utilizou o padrão Controller de forma adequada, promovendo a separação entre lógica de controle e demais responsabilidades?	

**Exemplos no projeto:**
- `AccountCommandController` (`src/FinanceApp.Api/Controllers/AccountCommandController.cs`) — recebe a requisição HTTP, delega imediatamente para `CreateAccountUseCase`, retorna resposta. Sem lógica de negócio.
- `AccountQueryController` — idem para consultas; delega para `GetAccountUseCase` e `GetAccountByDocumentUseCase`.
- `TransactionCommandController` — delega para `RegisterTransactionUseCase`; trata apenas o contrato HTTP.
- `TransactionQueryController` — delega para `ListTransactionsUseCase`; formata a resposta sem replicar regra de negócio.

### 4. Desenvolver testes unitários e aplicar TDD
#### 4.1 O aluno aplicou corretamente os princípios de testes unitários como isolamento, repetibilidade, rapidez, auto-verificação e abrangência?	

**Exemplos no projeto:**
- `MoneyTests` (`tests/FinanceApp.Domain.Tests/ValueObjects/MoneyTests.cs`) — testes isolados, sem I/O, determinísticos; verificam comportamento de `Money` para valores válidos e inválidos.
- `DocumentTests` — idem para o Value Object `Document`; cada teste é independente e auto-verificável via FluentAssertions.
- `CreateAccountUseCaseTests` (`tests/FinanceApp.Application.Tests/CreateAccountUseCaseTests.cs`) — usa `Mock<IAccountRepository>` para isolar o Use Case do banco de dados, garantindo rapidez e repetibilidade.

#### 4.2 O aluno implementou testes unitários abrangendo todos os métodos que contêm regras de negócio relevantes?	

**Exemplos no projeto:**
- `AccountTests` — cobre `HasBalance` e `UpdateBalance` (regras de saldo do agregado `Account`).
- `TransactionTests` — cobre invariantes do construtor de `Transaction`.
- `TransactionDomainServiceTests` — cobre `ValidateBalance` (regra cross-aggregate de saldo insuficiente).
- `RegisterTransactionUseCaseTests` — cobre o fluxo completo de registro de transação, incluindo cenários de saldo insuficiente, categoria inexistente e rollback de `IUnitOfWork`.
- `ListTransactionsUseCaseTests` — cobre cálculo de `TotalIncome`, `TotalExpense` e `NetBalance`.

#### 4.3 O aluno utilizou mocks e stubs de maneira adequada para isolar o código sob teste durante a implementação dos testes unitários?	

**Exemplos no projeto:**
- `RegisterTransactionUseCaseTests` — usa `Mock<IAccountRepository>`, `Mock<ITransactionRepository>`, `Mock<ICategoryACL>`, `Mock<ITransactionDomainService>` e `Mock<IUnitOfWork>` para isolar completamente o Use Case.
- `TransactionDomainServiceTests` — usa `Mock<IAccountRepository>` para simular cenários de conta inexistente e saldo insuficiente sem tocar no banco.
- `GetAccountUseCaseTests` — usa `Mock<IAccountRepository>` configurado com `.Setup(r => r.GetById(...)).ReturnsAsync(...)` para controlar o retorno em cada cenário.

#### 4.4 O aluno implementou testes unitários com cobertura superior a 80% do código de domínio, garantindo qualidade e confiabilidade da aplicação?	

**Exemplos no projeto:**
- Suíte de testes de domínio (`tests/FinanceApp.Domain.Tests/`) cobre `Account`, `Transaction`, `TransactionDomainService`, `Money`, `Document` e `CategoryRef`.
- Suíte de testes de aplicação (`tests/FinanceApp.Application.Tests/`) cobre todos os Use Cases: `CreateAccount`, `GetAccount`, `GetAccountByDocument`, `RegisterTransaction` e `ListTransactions`.
- Testes de integração (`tests/FinanceApp.Integration.Tests/`) complementam a cobertura com cenários E2E (`E2EFlowTests`) e ACL (`CategoryACLIntegrationTests`), garantindo que os fluxos completos também sejam validados.

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

---
