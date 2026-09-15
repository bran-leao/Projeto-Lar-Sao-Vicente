using System.ComponentModel.DataAnnotations;

namespace LarSaoVicente.Domain.Enums;

/// <summary>
/// Situação de uma entrada de medicamento.
/// </summary>
/// <remarks>
/// O estoque é a <b>soma das entradas conferidas</b>, e não um número transferido de um
/// lugar para outro. Uma entrada pendente simplesmente não é contada: não precisa ser
/// segurada em área nenhuma, e não existe transferência que possa falhar pela metade ou
/// ser esquecida. Ver <c>docs/arquitetura.md</c>, seção 5.2.
/// </remarks>
public enum MedicationEntryStatus
{
    /// <summary>Registrada, ainda não liberada para uso. Não conta no estoque.</summary>
    [Display(Name = "Aguardando conferência")]
    AguardandoConferencia = 1,

    /// <summary>Revisada e liberada. Conta no estoque.</summary>
    [Display(Name = "Conferida")]
    Conferida = 2,

    /// <summary>
    /// Revisada e recusada, com o motivo registrado. Não conta no estoque, mas permanece
    /// gravada: é histórico e é informação de gestão — permite responder quantas doações
    /// vencidas a instituição recebeu em um período.
    /// </summary>
    [Display(Name = "Recusada")]
    Recusada = 3
}
