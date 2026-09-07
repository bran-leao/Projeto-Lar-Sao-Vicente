using System.ComponentModel.DataAnnotations;

namespace LarSaoVicente.Domain.Enums;

/// <summary>
/// Situação do residente na instituição.
/// </summary>
/// <remarks>
/// A saída de um residente é registrada como <see cref="Inativo"/>. O sistema nunca
/// remove fisicamente o cadastro, pois o histórico precisa ser preservado para
/// consultas futuras e para os módulos de rastreabilidade previstos no backlog.
/// </remarks>
public enum ResidentStatus
{
    [Display(Name = "Ativo")]
    Ativo = 1,

    [Display(Name = "Inativo")]
    Inativo = 2
}
