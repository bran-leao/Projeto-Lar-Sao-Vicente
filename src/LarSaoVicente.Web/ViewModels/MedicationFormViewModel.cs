using System.ComponentModel.DataAnnotations;
using LarSaoVicente.Domain.Common;
using LarSaoVicente.Domain.Entities;
using LarSaoVicente.Domain.Enums;

namespace LarSaoVicente.Web.ViewModels;

/// <summary>
/// Formulário de cadastro e de edição de itens do catálogo (US09).
/// </summary>
/// <remarks>
/// A tela trabalha com este ViewModel e nunca com a entidade <see cref="Medication"/>,
/// para que um campo enviado indevidamente no formulário não alcance o banco.
/// </remarks>
public class MedicationFormViewModel
{
    public Guid? Id { get; set; }

    [Display(Name = "Nome do item")]
    [Required(ErrorMessage = "Informe o nome do item.")]
    [StringLength(Medication.CommercialNameMaxLength,
        ErrorMessage = "O nome deve ter no máximo {1} caracteres.")]
    public string CommercialName { get; set; } = string.Empty;

    /// <summary>
    /// Princípio ativo. Obrigatório para medicamento, ignorado para insumo.
    /// </summary>
    /// <remarks>
    /// A obrigatoriedade depende da categoria, por isso é verificada no controller e não
    /// por um atributo. É o campo que liga o mesmo remédio entre marcas: nas planilhas da
    /// instituição o mesmo item aparece ora pelo nome comercial, ora pelo princípio ativo,
    /// e nada relaciona os dois registros.
    /// </remarks>
    [Display(Name = "Princípio ativo")]
    [StringLength(Medication.ActiveIngredientMaxLength,
        ErrorMessage = "O princípio ativo deve ter no máximo {1} caracteres.")]
    public string? ActiveIngredient { get; set; }

    [Display(Name = "Concentração")]
    [StringLength(Medication.StrengthMaxLength,
        ErrorMessage = "A concentração deve ter no máximo {1} caracteres.")]
    public string? Strength { get; set; }

    [Display(Name = "Forma farmacêutica")]
    public PharmaceuticalForm? Form { get; set; }

    [Display(Name = "Quantidade por embalagem")]
    [Range(1, Medication.MaxUnitsPerPackage,
        ErrorMessage = "A quantidade por embalagem deve estar entre {1} e {2}.")]
    public int? UnitsPerPackage { get; set; }

    [Display(Name = "Unidade de embalagem")]
    [Required(ErrorMessage = "Selecione a unidade de embalagem.")]
    public PackageUnit? PackageUnit { get; set; }

    [Display(Name = "Código de barras")]
    [StringLength(Gtin.MaxLength + 6,
        ErrorMessage = "O código de barras deve ter no máximo {1} caracteres.")]
    public string? Barcode { get; set; }

    [Display(Name = "Categoria")]
    [Required(ErrorMessage = "Selecione a categoria.")]
    public MedicationCategory? Category { get; set; } = MedicationCategory.Medicamento;

    [Display(Name = "Observações")]
    [StringLength(Medication.NotesMaxLength,
        ErrorMessage = "As observações devem ter no máximo {1} caracteres.")]
    [DataType(DataType.MultilineText)]
    public string? Notes { get; set; }

    /// <summary>Lote lido no DataMatrix, preservado para a entrada que vem depois.</summary>
    public string? LoteLido { get; set; }

    /// <summary>Validade lida no DataMatrix, preservada para a entrada que vem depois.</summary>
    public DateOnly? ValidadeLida { get; set; }

    /// <summary>Número de série lido no DataMatrix.</summary>
    public string? SerieLida { get; set; }

    /// <summary>
    /// Indica que o cadastro começou por uma leitura de código.
    /// </summary>
    /// <remarks>
    /// Quando verdadeiro, ao salvar o item o sistema segue direto para o registro da
    /// entrada, já com lote e validade preenchidos — que é o caminho que a enfermagem
    /// percorre no balcão, com a caixa na mão.
    /// </remarks>
    public bool VeioDeLeitura { get; set; }

    public bool EhEdicao => Id.HasValue;

    public static MedicationFormViewModel DeEntidade(Medication item) => new()
    {
        Id = item.Id,
        CommercialName = item.CommercialName,
        ActiveIngredient = item.ActiveIngredient,
        Strength = item.Strength,
        Form = item.Form,
        UnitsPerPackage = item.UnitsPerPackage,
        PackageUnit = item.PackageUnit,
        Barcode = item.Barcode,
        Category = item.Category,
        Notes = item.Notes
    };
}
