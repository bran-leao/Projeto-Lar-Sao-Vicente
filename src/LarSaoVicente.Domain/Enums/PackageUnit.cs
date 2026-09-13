using System.ComponentModel.DataAnnotations;

namespace LarSaoVicente.Domain.Enums;

/// <summary>
/// Unidade em que o item é recebido e contado no estoque.
/// </summary>
/// <remarks>
/// Campo obrigatório: na planilha da instituição a quantidade aparece como "17CX" ou
/// "1FR", e um número sem unidade não significa nada — trinta pode ser trinta
/// comprimidos ou trinta caixas.
/// <para>
/// Caixa e frasco cobrem os 144 itens do catálogo atual (130 e 14, respectivamente).
/// As demais constam do vocabulário levantado com a equipe e são usadas sobretudo
/// pelos insumos.
/// </para>
/// </remarks>
public enum PackageUnit
{
    [Display(Name = "Caixa")]
    Caixa = 1,

    [Display(Name = "Frasco")]
    Frasco = 2,

    [Display(Name = "Tubo")]
    Tubo = 3,

    [Display(Name = "Sachê")]
    Sache = 4,

    [Display(Name = "Fardo")]
    Fardo = 5,

    [Display(Name = "Caixa master")]
    CaixaMaster = 6,

    [Display(Name = "Unidade")]
    Unidade = 7
}
