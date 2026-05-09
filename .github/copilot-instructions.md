# Copilot Instructions

Read `PLAN.md` before starting any task.

## Non-negotiable rules
- Never reference `Account` object inside `Transaction` — use `AccountId: Guid` only (Model B)
- Always create the interface before the implementation
- `Money` and `CategoryRef` must always be immutable `record` types
- Every class generated must have a corresponding unit test in the same response
- Domain exceptions only — never throw `Exception`, `ArgumentException` or `InvalidOperationException` directly
- All repository and UseCase methods must use `async/await`

## Architecture
- `FinanceApp.Domain` has zero external dependencies
- `FinanceApp.Application` depends only on domain interfaces
- `FinanceApp.Infrastructure` implements domain interfaces — never the other way around
- `CategoryACL` is the only class allowed to reference `ICategoryRepository` from the Catalog Context

## Test pattern
- Framework: xUnit + Moq + FluentAssertions
- Method naming: `Deve[Result]_Quando[Condition]`
- Structure: Arrange / Act / Assert (commented)