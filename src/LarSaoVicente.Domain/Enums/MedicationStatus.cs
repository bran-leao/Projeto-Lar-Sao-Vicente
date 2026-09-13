using System.ComponentModel.DataAnnotations;

namespace LarSaoVicente.Domain.Enums;

/// <summary>
/// Situação do item no catálogo.
/// </summary>
/// <remarks>
/// Segue a mesma regra do cadastro de residentes: um item que a instituição deixa de
/// usar é inativado, nunca excluído. As entradas e as administrações já registradas
/// apontam para ele, e apagá-lo destruiria o histórico que o sistema existe para manter.
/// </remarks>
public enum MedicationStatus
{
    [Display(Name = "Ativo")]
    Ativo = 1,

    [Display(Name = "Inativo")]
    Inativo = 2
}
