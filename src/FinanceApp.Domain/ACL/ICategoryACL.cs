using FinanceApp.Domain.ValueObjects;

namespace FinanceApp.Domain.ACL;

// [SOLID: DIP] — O Financial Context depende desta interface (definida no Domain),
// não de CategoryACL (definida na Infrastructure). A seta de dependência aponta para dentro.
// Objetivo: a camada de domínio nunca referencia implementações de infraestrutura.
//
// [GRASP: Indirection] — ICategoryACL age como intermediário entre Financial e Catalog Context.
// Objetivo: desacoplar dois módulos que não devem se conhecer diretamente,
// introduzindo um ponto de indireção estável (a interface).
/// <summary> Anti-Corruption Layer — traduz Category do Catalog Context para CategoryRef do Financial Context. </summary>
public interface ICategoryACL
{
    Task<CategoryRef> GetCategory(string name);
}
