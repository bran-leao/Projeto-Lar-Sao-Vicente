using System.ComponentModel.DataAnnotations;

namespace LarSaoVicente.Domain.Enums;

/// <summary>
/// Grau de dependência do residente, conforme a classificação utilizada pelas
/// Instituições de Longa Permanência para Idosos.
/// </summary>
/// <remarks>
/// O atributo <see cref="DisplayAttribute"/> define o rótulo exibido na interface.
/// Os valores numéricos são explícitos porque são persistidos no banco de dados:
/// alterá-los invalidaria os registros existentes.
/// </remarks>
public enum DependencyLevel
{
    /// <summary>Idosos independentes, mesmo que requeiram uso de equipamentos de autoajuda.</summary>
    [Display(Name = "Grau I")]
    GrauI = 1,

    /// <summary>Idosos com dependência em até três atividades de autocuidado.</summary>
    [Display(Name = "Grau II")]
    GrauII = 2,

    /// <summary>Idosos com dependência que requeiram assistência em todas as atividades de autocuidado.</summary>
    [Display(Name = "Grau III")]
    GrauIII = 3
}
