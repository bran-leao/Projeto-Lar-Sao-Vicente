using System.ComponentModel.DataAnnotations;

namespace LarSaoVicente.Domain.Enums;

/// <summary>
/// De onde o medicamento veio.
/// </summary>
/// <remarks>
/// São cinco caminhos, e não dois. O desenho inicial previa apenas compra e doação; as
/// planilhas da instituição mostraram cinco origens reais
/// (<c>docs/requisitos.md</c>, seção 2.1).
/// <para>
/// A distinção importa além do registro: o que a família traz pertence àquele residente
/// e não deve ser consumido por outro; o da Farmácia Popular e o da UBS têm prazo de
/// retirada; o de doação é o que mais chega perto do vencimento. São regras diferentes
/// sobre a mesma entrada, e só existem se a origem for um campo, não uma observação.
/// </para>
/// </remarks>
public enum MedicationOrigin
{
    [Display(Name = "Distribuidora")]
    Distribuidora = 1,

    [Display(Name = "Farmácia Popular")]
    FarmaciaPopular = 2,

    [Display(Name = "Posto de saúde (UBS)")]
    UnidadeBasicaDeSaude = 3,

    [Display(Name = "Família do residente")]
    Familia = 4,

    [Display(Name = "Doação")]
    Doacao = 5
}
