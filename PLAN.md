# 💰 Sistema de Controle de Finanças Pessoais — AI Blueprint

> **Como usar este arquivo:** No início de cada sessão com a IA, cole este documento e diga:
> *"Leia o PLANO.md abaixo, use-o como contexto para todo o projeto e confirme que entendeu listando os Bounded Contexts e as regras de negócio."*

---

## 1. Visão Geral do Projeto

| Item            | Descrição                                                                          |
|-----------------|------------------------------------------------------------------------------------|
| **Nome**        | FinanceApp — Sistema de Controle de Finanças Pessoais                              |
| **Objetivo**    | Permitir que o usuário registre receitas e despesas, organize por categorias, defina orçamentos mensais e acompanhe relatórios financeiros |
| **Linguagem**   | C# 13                                                                              |
| **Framework**   | .NET 10 + ASP.NET Core (Web API)                                                   |
| **ORM**         | Entity Framework Core 10 com provider Npgsql                                       |
| **Banco**       | PostgreSQL                                                                         |
| **Arquitetura** | DDD (Domain-Driven Design)                                                         |
| **Princípios**  | SOLID + GRASP                                                                      |
| **Testes**      | xUnit + Moq + FluentAssertions (cobertura mínima: 80%)                             |
| **Build**       | dotnet CLI / `.csproj`                                                             |

---

## 2. Bounded Contexts (DDD)

O sistema é dividido em **2 contextos delimitados**:

### 2.1 `Financial Context` — downstream
Responsável por registrar e gerenciar contas e movimentações financeiras.
Contém os agregados `Account` e `Transaction`, além da `CategoryACL` que protege este contexto do modelo externo.

### 2.2 `Catalog Context` — upstream
Responsável por gerenciar categorias de transações.
Expõe `Category` via `ICategoryRepository`, consumido pela `CategoryACL` do Financial Context.

### Context Map
```
Catalog Context (upstream)
        │
        │  CategoryACL (Anti-Corruption Layer)
        ▼
Financial Context (downstream)
```

> ⚠️ **Regra de integração:** O Financial Context nunca acessa diretamente entidades do Catalog Context. A `CategoryACL` traduz `Category` para o Value Object interno `CategoryRef`.

---

## 3. Domínio — Entidades, Value Objects e Regras de Negócio

### 3.1 `Financial Context` — Agregados

#### Aggregate Root: `Account`

| Atributo  | Tipo C#  | Descrição                              |
|-----------|----------|----------------------------------------|
| Id        | Guid     | Identificador único                    |
| Name      | string   | Nome do titular                        |
| Document  | string   | Documento de identificação (CPF/CNPJ)  |
| Balance   | Money    | Saldo atual da conta (Value Object)    |

**Métodos de domínio:**
- `UpdateBalance(amount: Money, type: TransactionType)` — atualiza o saldo após uma transação ser registrada
- `HasBalance(amount: Money): bool` — verifica se a conta possui saldo suficiente para uma despesa

**Regras de negócio:**
- `Document` deve ser único no sistema
- `Balance` nunca pode ficar negativo — `HasBalance()` deve ser verificado antes de registrar uma despesa
- `Balance` é atualizado pelo `AppService` após persistir a `Transaction`, chamando `UpdateBalance()`

---

#### Aggregate Root: `Transaction`

| Atributo    | Tipo C#         | Descrição                                          |
|-------------|-----------------|----------------------------------------------------|
| Id          | Guid            | Identificador único                                |
| AccountId   | Guid            | Referência à conta por ID (sem navegação por objeto)|
| Date        | DateTime        | Data e hora da transação                           |
| CategoryRef | CategoryRef     | Referência à categoria (Value Object interno)      |
| Type        | TransactionType | Income ou Expense (enum)                           |
| Amount      | Money           | Valor da transação (Value Object)                  |

**Regras de negócio:**
- `Amount` deve ser sempre positivo — lançar `TransactionException` se zero ou negativo
- `AccountId` é imutável após criação — uma transação não muda de conta
- `Date` não pode ser futura
- `Transaction` referencia `Account` apenas por `AccountId: Guid`, nunca por objeto — Modelo B (agregados independentes)

> **Decisão de design — Modelo B:** `Transaction` é um Aggregate Root independente. Não existe `TransactionFactory` por ora, pois a criação ainda não tem complexidade suficiente. Quando a lógica crescer, uma factory será introduzida (GRASP Creator).

---

### 3.2 Value Objects

| Value Object  | Atributos C#              | Regras de validação                               |
|---------------|---------------------------|---------------------------------------------------|
| `Money`       | Value: decimal            | Deve ser positivo; 2 casas decimais               |
| `CategoryRef` | CategoryId: Guid, Name: string | CategoryId não pode ser Guid.Empty; Name obrigatório |

> Value Objects são **imutáveis**. Usar `record` do C#.

```csharp
public record Money
{
    public decimal Value { get; }

    public Money(decimal value)
    {
        if (value <= 0)
            throw new DomainException("O valor deve ser positivo.");

        Value = Math.Round(value, 2);
    }
}

public record CategoryRef(Guid CategoryId, string Name)
{
    public Guid CategoryId { get; } = CategoryId == Guid.Empty
        ? throw new DomainException("CategoryId inválido.")
        : CategoryId;

    public string Name { get; } = string.IsNullOrWhiteSpace(Name)
        ? throw new DomainException("Nome da categoria é obrigatório.")
        : Name;
}
```

---

### 3.3 Enum: `TransactionType`
```csharp
public enum TransactionType { Income, Expense }
```

---

### 3.4 `Catalog Context` — Agregado

#### Aggregate Root: `Category`

| Atributo | Tipo C# | Descrição               |
|----------|---------|-------------------------|
| Id       | Guid    | Identificador único     |
| Name     | string  | Nome da categoria       |

**Regras de negócio:**
- `Name` é obrigatório e deve ser único no sistema
- `Category` não possui `Factory` — criação simples via construtor

---

### 3.5 Anti-Corruption Layer: `CategoryACL`

Pertence ao **Financial Context**. Responsável por:
- Chamar `ICategoryRepository` para buscar `Category` no Catalog Context
- Traduzir `Category` para `CategoryRef` (VO interno do Financial Context)
- Proteger o Financial Context de mudanças no modelo do Catalog Context

> **Decisão de design:** `CategoryACL` deve implementar a interface `ICategoryACL` para respeitar o DIP e permitir mock nos testes de UseCase.

```csharp
// Interface — definida no domínio do Financial Context
public interface ICategoryACL
{
    Task<CategoryRef> GetCategory(string name);
}

// Implementação
public class CategoryACL : ICategoryACL
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryACL(ICategoryRepository categoryRepository)
        => _categoryRepository = categoryRepository;

    public async Task<CategoryRef> GetCategory(string name)
    {
        var category = await _categoryRepository.GetByName(name)
            ?? throw new CategoryException($"Categoria '{name}' não encontrada.");

        return new CategoryRef(category.Id, category.Name);
    }
}
```

---

## 4. Interfaces de Repositório

### Financial Context

```csharp
// IAccountRepository — domínio
public interface IAccountRepository
{
    Task<Account?> GetById(Guid accountId);
    Task<Account?> GetByDocument(string document);
    Task Save(Account account);
}

// ITransactionRepository — domínio
public interface ITransactionRepository
{
    Task Add(Transaction transaction);
    Task<List<Transaction>> ListByAccountId(Guid accountId);
    Task<List<Transaction>> ListByAccountIdAndPeriod(Guid accountId, DateTime startDate, DateTime endDate);
}
```

### Catalog Context

```csharp
// ICategoryRepository — domínio do Catalog Context
public interface ICategoryRepository
{
    Task<Category?> GetByName(string name);
    Task<List<Category>> ListAll();
}
```

---

## 5. Estrutura de Projetos (Solution)

```
FinanceApp.sln
│
├── src/
│   ├── FinanceApp.Domain/                         # Domínio — Financial Context
│   │   ├── Account/
│   │   │   ├── Account.cs                         # <<AggregateRoot>>
│   │   │   └── IAccountRepository.cs
│   │   ├── Transaction/
│   │   │   ├── Transaction.cs                     # <<AggregateRoot>>
│   │   │   ├── TransactionType.cs                 # <<ENUM>>
│   │   │   ├── ITransactionRepository.cs
│   │   │   ├── ITransactionDomainService.cs       # Interface do DomainService (DIP)
│   │   │   └── TransactionDomainService.cs
│   │   ├── ValueObjects/
│   │   │   ├── Money.cs                           # <<ValueObject>>
│   │   │   └── CategoryRef.cs                     # <<ValueObject>>
│   │   ├── ACL/
│   │   │   └── ICategoryACL.cs                    # Interface da ACL (DIP)
│   │   └── Exceptions/
│   │       ├── DomainException.cs
│   │       ├── AccountException.cs
│   │       └── TransactionException.cs
│   │
│   ├── FinanceApp.Catalog/                        # Domínio — Catalog Context
│   │   ├── Category/
│   │   │   ├── Category.cs                        # <<AggregateRoot>>
│   │   │   └── ICategoryRepository.cs
│   │   └── Exceptions/
│   │       └── CategoryException.cs
│   │
│   ├── FinanceApp.Application/                    # Camada de Aplicação
│   │   ├── Interfaces/
│   │   │   └── IUnitOfWork.cs                     # Unit of Work — orquestra transação de banco
│   │   ├── Transaction/
│   │   │   ├── RegisterTransactionUseCase.cs
│   │   │   ├── ListTransactionsUseCase.cs
│   │   │   └── TransactionAppService.cs           # Orquestra repositórios + ACL + DomainService
│   │   └── Account/
│   │       ├── CreateAccountUseCase.cs
│   │       └── GetAccountUseCase.cs
│   │
│   ├── FinanceApp.Infrastructure/                 # Infraestrutura
│   │   ├── ACL/
│   │   │   └── CategoryACL.cs                     # Implementação da ICategoryACL (referencia Catalog)
│   │   ├── Persistence/
│   │   │   ├── FinanceAppDbContext.cs
│   │   │   ├── AccountRepository.cs               # implements IAccountRepository
│   │   │   ├── TransactionRepository.cs           # implements ITransactionRepository
│   │   │   ├── CategoryRepository.cs              # implements ICategoryRepository
│   │   │   └── Configurations/
│   │   │       ├── AccountConfiguration.cs
│   │   │       ├── TransactionConfiguration.cs
│   │   │       └── CategoryConfiguration.cs
│   │   └── DependencyInjection.cs
│   │
│   └── FinanceApp.Api/                            # Apresentação
│       ├── Controllers/
│       │   ├── TransactionController.cs
│       │   ├── AccountController.cs
│       │   └── CategoryController.cs
│       ├── Middlewares/
│       │   └── ExceptionHandlingMiddleware.cs
│       ├── Program.cs
│       └── appsettings.json
│
└── tests/
    ├── FinanceApp.Domain.Tests/
    │   ├── AccountTests.cs
    │   ├── TransactionTests.cs
    │   └── ValueObjects/
    │       ├── MoneyTests.cs
    │       └── CategoryRefTests.cs
    ├── FinanceApp.Application.Tests/
    │   ├── RegisterTransactionUseCaseTests.cs
    │   └── CreateAccountUseCaseTests.cs
    └── FinanceApp.Infrastructure.Tests/
        ├── AccountRepositoryTests.cs             # Integração com Testcontainers
        └── TransactionRepositoryTests.cs
```

---

## 6. Princípios a Aplicar

### 6.1 SOLID

| Princípio | Aplicação no projeto                                                                                     |
|-----------|----------------------------------------------------------------------------------------------------------|
| **SRP**   | `RegisterTransactionUseCase` só registra; `TransactionDomainService` só valida regras de domínio        |
| **OCP**   | Novos tipos de relatório podem ser adicionados sem alterar os existentes via interface de serviço        |
| **LSP**   | `AccountRepository` e `TransactionRepository` são intercambiáveis com implementações in-memory em testes|
| **ISP**   | `IAccountRepository` e `ITransactionRepository` são interfaces específicas sem métodos desnecessários    |
| **DIP**   | `TransactionAppService` depende de `IAccountRepository` e `ITransactionRepository`, não das implementações EF Core |

### 6.2 GRASP

| Padrão               | Aplicação no projeto                                                                          |
|----------------------|-----------------------------------------------------------------------------------------------|
| **Expert**           | `Account.HasBalance()` — a conta conhece seu próprio saldo e sabe se pode cobrir uma despesa  |
| **Expert**           | `Account.UpdateBalance()` — a conta atualiza seu próprio saldo                                |
| **Creator**          | `TransactionAppService` instancia `Transaction` diretamente (sem factory por ora)             |
| **Controller**       | `TransactionAppService` orquestra o fluxo; `TransactionController` da API apenas delega       |
| **Low Coupling**     | Financial e Catalog Context se comunicam apenas via `CategoryACL` e `ICategoryRepository`     |
| **High Cohesion**    | Cada classe agrupada por responsabilidade dentro do seu bounded context                       |
| **Pure Fabrication** | `CategoryACL` — classe artificial que não representa conceito do domínio, criada para isolar a integração |

---

## 7. Fluxo do `RegisterTransactionUseCase` (Modelo B)

O fluxo ao registrar uma transação demonstra o Modelo B na prática:

```
1. TransactionDomainService.ValidateBalance(accountId, amount)
      └── IAccountRepository.GetById(accountId) → lança AccountException se não encontrar
      └── account.HasBalance(amount) → lança TransactionException se insuficiente

2. categoryRef = ICategoryACL.GetCategory(categoryName)
      └── ICategoryRepository.GetByName(name)
      └── traduz Category → CategoryRef

3. date = parâmetro recebido ?? DateTime.UtcNow

4. transaction = new Transaction(accountId, date, categoryRef, type, amount)

5. Dentro de IUnitOfWork:
   ├── ITransactionRepository.Add(transaction)
   ├── account = IAccountRepository.GetById(accountId)
   ├── account.UpdateBalance(amount, type)
   └── IAccountRepository.Save(account)
```

> **Consistência:** os passos do bloco 5 devem ocorrer dentro de uma **Unit of Work** (`IUnitOfWork`) para garantir que o saldo não fique inconsistente se a aplicação falhar entre os dois saves. `IUnitOfWork` é declarado em `FinanceApp.Application` e implementado na Infrastructure.

> **Data da transação:** o UseCase recebe `DateTime? date = null`. Quando `null`, é substituído por `DateTime.UtcNow` antes de criar a `Transaction`.

---

## 8. Exceções de Domínio

| Exceção               | Quando lançar                                                             |
|-----------------------|---------------------------------------------------------------------------|
| `DomainException`     | Classe base — erros gerais de regra de negócio                            |
| `AccountException`    | Documento duplicado, conta não encontrada, operação inválida na conta     |
| `TransactionException`| Valor negativo ou zero, data futura, AccountId inválido                   |
| `CategoryException`   | Categoria não encontrada, nome inválido                                   |

```csharp
public class DomainException(string message) : Exception(message);
public class AccountException(string message) : DomainException(message);
public class TransactionException(string message) : DomainException(message);
public class CategoryException(string message) : DomainException(message);
```

> `AccountException` reside em `FinanceApp.Domain/Exceptions/` junto com as demais.

---

## 9. Critérios de Geração de Código

Ao gerar qualquer classe, a IA **DEVE**:

- [ ] Criar a **interface** antes da implementação (DIP)
- [ ] Usar `record` para todos os **Value Objects** (`Money`, `CategoryRef`)
- [ ] Usar `enum` para `TransactionType`
- [ ] Usar estereótipos nos comentários XML: `/// <summary> <<AggregateRoot>> </summary>`
- [ ] Lançar apenas **exceções de domínio customizadas** (nunca `Exception`, `ArgumentException` ou `InvalidOperationException` diretamente)
- [ ] Registrar dependências no `DependencyInjection.cs`
- [ ] Gerar o **teste unitário correspondente na mesma resposta**
- [ ] Usar **async/await** em todos os métodos de repositório e UseCase
- [ ] `Transaction` referencia `Account` apenas por `AccountId: Guid` — nunca por objeto
- [ ] `CategoryACL` deve ser injetada como `ICategoryACL` em todos os UseCases
- [ ] `RegisterTransactionUseCase` aceita `DateTime? date = null` — usa `DateTime.UtcNow` quando nulo
- [ ] Operações de escrita no UseCase devem ser envoltas por `IUnitOfWork`

---

## 10. Padrão de Testes

### 10.1 Regras gerais
- Framework: **xUnit**
- Mock: **Moq**
- Asserções: **FluentAssertions**
- Nomear métodos: `Deve[Resultado]_Quando[Condicao]`
- Estrutura: **Arrange / Act / Assert**
- Testes de integração: **Testcontainers** (PostgreSQL real em container)

### 10.2 Exemplos de nomes

```csharp
// Account
DeveAtualizarSaldo_QuandoTransacaoDeReceitaForRegistrada()
DeveLancarExcecao_QuandoSaldoForInsuficienteParaDespesa()

// Transaction
DeveLancarTransactionException_QuandoValorForNegativo()
DeveLancarTransactionException_QuandoDataForFutura()
DeveCriarTransacao_QuandoDadosValidos()

// Money (Value Object)
DeveLancarDomainException_QuandoValorForZero()
DeveLancarDomainException_QuandoValorForNegativo()
DeveArredondarParaDuasCasasDecimais_QuandoCriado()

// CategoryRef (Value Object)
DeveLancarDomainException_QuandoCategoryIdForVazio()
DeveLancarDomainException_QuandoNomeForNulo()

// UseCase
DeveRegistrarTransacao_ComSucesso()
DeveLancarExcecao_QuandoContaNaoExistir()
DeveLancarExcecao_QuandoSaldoForInsuficiente()
DeveLancarExcecao_QuandoCategoriaForInvalida()
```

### 10.3 Estrutura base — teste de Value Object

```csharp
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
    public void DeveArredondarParaDuasCasasDecimais_QuandoCriado()
    {
        // Arrange & Act
        var money = new Money(10.999m);

        // Assert
        money.Value.Should().Be(11.00m);
    }
}
```

### 10.4 Estrutura base — teste de UseCase com Moq

```csharp
public class RegisterTransactionUseCaseTests
{
    private readonly Mock<IAccountRepository> _accountRepoMock = new();
    private readonly Mock<ITransactionRepository> _transactionRepoMock = new();
    private readonly Mock<ICategoryACL> _categoryAclMock = new();   // ICategoryACL — não CategoryACL
    private readonly Mock<TransactionDomainService> _domainServiceMock = new();
    private readonly RegisterTransactionUseCase _useCase;

    public RegisterTransactionUseCaseTests()
    {
        _useCase = new RegisterTransactionUseCase(
            _accountRepoMock.Object,
            _transactionRepoMock.Object,
            _categoryAclMock.Object,
            _domainServiceMock.Object
        );
    }

    [Fact]
    public async Task DeveRegistrarTransacao_ComSucesso()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = new Account(accountId, "João", "12345678900", new Money(1000m));
        var categoryRef = new CategoryRef(Guid.NewGuid(), "Alimentação");

        _accountRepoMock.Setup(r => r.GetById(accountId)).ReturnsAsync(account);
        _categoryAclMock.Setup(a => a.GetCategory("Alimentação")).ReturnsAsync(categoryRef);
        _transactionRepoMock.Setup(r => r.Add(It.IsAny<Transaction>())).Returns(Task.CompletedTask);
        _accountRepoMock.Setup(r => r.Save(It.IsAny<Account>())).Returns(Task.CompletedTask);

        // Act — date null → sistema usa DateTime.UtcNow
        await _useCase.ExecuteAsync(accountId, 100m, TransactionType.Expense, "Alimentação", date: null);

        // Assert
        _transactionRepoMock.Verify(r => r.Add(It.IsAny<Transaction>()), Times.Once);
        _accountRepoMock.Verify(r => r.Save(It.IsAny<Account>()), Times.Once);
    }
}
```

---

## 11. Configuração de Banco (EF Core + PostgreSQL)

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=financeapp;Username=postgres;Password=senha"
  }
}
```

```csharp
public class FinanceAppDbContext(DbContextOptions<FinanceAppDbContext> options)
    : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FinanceAppDbContext).Assembly);
    }
}
```

> Migrations:
> ```bash
> dotnet ef migrations add InitialCreate --project FinanceApp.Infrastructure --startup-project FinanceApp.Api
> dotnet ef database update --project FinanceApp.Infrastructure --startup-project FinanceApp.Api
> ```

---

## 12. Sequência de Desenvolvimento (Prompts Sugeridos)

### Fase 1 — Domínio

```
Prompt 1.1 — Exceções e Value Objects:
"Com base no PLANO.md, crie a hierarquia de exceções (DomainException, TransactionException,
CategoryException). Em seguida, crie os Value Objects Money e CategoryRef como records C#
com as validações descritas. Gere os testes unitários com xUnit e FluentAssertions."

Prompt 1.2 — Aggregate Account:
"Crie o Aggregate Root Account com os atributos do PLANO.md.
Implique os métodos UpdateBalance(amount: Money, type: TransactionType) e HasBalance(amount: Money).
Crie a interface IAccountRepository. Documente com XML doc comments e <<AggregateRoot>>.
Gere os testes unitários cobrindo UpdateBalance e HasBalance."

Prompt 1.3 — Aggregate Transaction:
"Crie o Aggregate Root Transaction com os atributos do PLANO.md.
Transaction referencia Account apenas por AccountId: Guid (Modelo B — sem navegação por objeto).
Crie a interface ITransactionRepository com os métodos: Add, ListByAccountId e ListByAccountIdAndPeriod.
Gere os testes unitários."

Prompt 1.4 — TransactionDomainService, ICategoryACL e CategoryACL:
"Crie o TransactionDomainService com ValidateBalance(accountId, amount) que usa IAccountRepository
e chama account.HasBalance(amount) — lança AccountException se conta não encontrada,
lança TransactionException se saldo insuficiente.
Crie a interface ICategoryACL e a implementação CategoryACL que depende de ICategoryRepository
e traduz Category para CategoryRef — lança CategoryException se não encontrada.
Crie ICategoryRepository com GetByName e ListAll. Gere os testes com Moq."
```

### Fase 2 — Application

```
Prompt 2.1 — IUnitOfWork e RegisterTransactionUseCase:
"Crie a interface IUnitOfWork em FinanceApp.Application/Interfaces/.
Crie o RegisterTransactionUseCase seguindo o fluxo do PLANO.md (seção 7).
Deve depender de IAccountRepository, ITransactionRepository, ICategoryACL,
TransactionDomainService e IUnitOfWork.
Aceita DateTime? date = null — usa DateTime.UtcNow quando nulo.
Use async/await. Gere os testes com Moq cobrindo: sucesso, conta inexistente,
saldo insuficiente, categoria inválida e data nula (deve usar data atual)."

Prompt 2.2 — ListTransactionsUseCase:
"Crie o ListTransactionsUseCase que lista transações de uma conta por período opcional,
usando ITransactionRepository.ListByAccountIdAndPeriod ou ListByAccountId.
Gere os testes com Moq."

Prompt 2.3 — CreateAccountUseCase e GetAccountUseCase:
"Crie o CreateAccountUseCase que valida Document único via IAccountRepository.GetByDocument()
antes de persistir — lança AccountException se duplicado. Crie o GetAccountUseCase.
Gere os testes com Moq."
```

### Fase 3 — Infraestrutura e API

```
Prompt 3.1 — DbContext e Repositories:
"Crie o FinanceAppDbContext com EF Core + Npgsql.
Implemente AccountRepository, TransactionRepository e CategoryRepository.
Use IEntityTypeConfiguration para mapear Money e CategoryRef como owned types.
Implemente IUnitOfWork usando EF Core DbContext (SaveChangesAsync)."

Prompt 3.2 — DI e Program.cs:
"Crie DependencyInjection.cs registrando todos os repositórios, UseCases, ICategoryACL/CategoryACL,
IUnitOfWork e TransactionDomainService. Configure Program.cs com Swagger e ExceptionHandlingMiddleware
que mapeia DomainException → HTTP 400, AccountException → HTTP 404, CategoryException → HTTP 404."

Prompt 3.3 — Controllers:
"Crie TransactionController, AccountController e CategoryController com [ApiController].
CategoryController expõe apenas GET (listagem — somente leitura).
Cada controller delega para o UseCase correspondente (GRASP Controller).
Documente com XML comments para o Swagger."
```

---

## 13. Glossário do Domínio

| Termo         | Significado no contexto do sistema                                      |
|---------------|-------------------------------------------------------------------------|
| Account       | Conta financeira do usuário, Aggregate Root do Financial Context        |
| Transaction   | Movimentação financeira (Income ou Expense), Aggregate Root independente|
| Income        | Transação que representa entrada de dinheiro                            |
| Expense       | Transação que representa saída de dinheiro                              |
| Money         | Value Object que representa um valor monetário positivo                 |
| CategoryRef   | Value Object interno que representa uma categoria no Financial Context  |
| Category      | Aggregate Root do Catalog Context — classificação das transações        |
| CategoryACL   | Anti-Corruption Layer — traduz Category para CategoryRef; implementa ICategoryACL |
| ICategoryACL  | Interface da ACL — permite injeção e mock nos testes de UseCase               |
| IUnitOfWork   | Abstrai a transação de banco — declarado em Application, implementado em Infrastructure |
| Balance       | Saldo atual de uma conta — resultado de Income menos Expense            |
| Document      | Identificador do titular da conta (CPF/CNPJ)                           |