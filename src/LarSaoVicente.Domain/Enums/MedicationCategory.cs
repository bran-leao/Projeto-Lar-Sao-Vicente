using System.ComponentModel.DataAnnotations;

namespace LarSaoVicente.Domain.Enums;

/// <summary>
/// Separa medicamentos de insumos dentro do mesmo catálogo.
/// </summary>
/// <remarks>
/// A distinção é da própria instituição: nas planilhas em uso, luvas, fraldas, lancetas
/// e materiais de procedimento são controlados em aba separada dos medicamentos
/// (ver <c>docs/requisitos.md</c>, seção 2.1).
/// <para>
/// Optou-se por um único cadastro com categoria, e não por dois módulos, porque entrada,
/// conferência, estoque e alerta de vencimento funcionam igual para os dois casos.
/// Duplicar as telas duplicaria também os defeitos.
/// </para>
/// </remarks>
public enum MedicationCategory
{
    [Display(Name = "Medicamento")]
    Medicamento = 1,

    /// <summary>Material de consumo: luvas, fraldas, lancetas,materiais de procedimento.</summary>
    [Display(Name = "Insumo")]
    Insumo = 2
}
