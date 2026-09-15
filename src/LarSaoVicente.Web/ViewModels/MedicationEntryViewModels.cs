using System.ComponentModel.DataAnnotations;
using LarSaoVicente.Domain.Entities;
using LarSaoVicente.Domain.Enums;

namespace LarSaoVicente.Web.ViewModels;

/// <summary>
/// Formulário de registro de uma remessa recebida (US10 e US26).
/// </summary>
public class MedicationEntryFormViewModel
{
    public Guid? Id { get; set; }

    [Required]
    public Guid MedicationId { get; set; }

    /// <summary>Nome do item, apenas para exibição — não é enviado de volta como dado.</summary>
    public string? NomeDoMedicamento { get; set; }

    /// <summary>Unidade definida no catálogo, exibida ao lado da quantidade.</summary>
    public PackageUnit? UnidadeDoMedicamento { get; set; }

    [Display(Name = "Quantidade recebida")]
    [Required(ErrorMessage = "Informe quantas unidades chegaram.")]
    [Range(1, MedicationEntry.MaxQuantity,
        ErrorMessage = "A quantidade deve estar entre {1} e {2}.")]
    public int? Quantity { get; set; } = 1;

    [Display(Name = "Lote")]
    [StringLength(MedicationEntry.LotNumberMaxLength,
        ErrorMessage = "O lote deve ter no máximo {1} caracteres.")]
    public string? LotNumber { get; set; }

    [Display(Name = "Validade")]
    [DataType(DataType.Date)]
    public DateOnly? ExpiryDate { get; set; }

    [Display(Name = "Não foi possível identificar a validade na embalagem")]
    public bool ExpiryNotIdentified { get; set; }

    [Display(Name = "Origem")]
    [Required(ErrorMessage = "Selecione a origem.")]
    public MedicationOrigin? Origin { get; set; }

    [Display(Name = "Data de recebimento")]
    [Required(ErrorMessage = "Informe a data de recebimento.")]
    [DataType(DataType.Date)]
    public DateOnly? ReceivedOn { get; set; }

    [Display(Name = "Número de série")]
    [StringLength(MedicationEntry.SerialNumberMaxLength,
        ErrorMessage = "O número de série deve ter no máximo {1} caracteres.")]
    public string? SerialNumber { get; set; }

    [Display(Name = "Observações")]
    [StringLength(MedicationEntry.NotesMaxLength,
        ErrorMessage = "As observações devem ter no máximo {1} caracteres.")]
    [DataType(DataType.MultilineText)]
    public string? Notes { get; set; }

    /// <summary>Indica que os dados vieram de uma leitura de código.</summary>
    public bool IdentifiedByScan { get; set; }

    public bool EhEdicao => Id.HasValue;

    public static MedicationEntryFormViewModel DeEntidade(MedicationEntry entrada, Medication item) => new()
    {
        Id = entrada.Id,
        MedicationId = entrada.MedicationId,
        NomeDoMedicamento = item.GetDisplayName(),
        UnidadeDoMedicamento = item.PackageUnit,
        Quantity = entrada.Quantity,
        LotNumber = entrada.LotNumber,
        ExpiryDate = entrada.ExpiryDate,
        ExpiryNotIdentified = entrada.ExpiryNotIdentified,
        Origin = entrada.Origin,
        ReceivedOn = entrada.ReceivedOn,
        SerialNumber = entrada.SerialNumber,
        Notes = entrada.Notes,
        IdentifiedByScan = entrada.IdentifiedByScan
    };
}

/// <summary>Uma linha na lista de entradas.</summary>
public class MedicationEntryListItemViewModel
{
    public required Guid Id { get; init; }

    public required Guid MedicationId { get; init; }

    public string? NomeDoMedicamento { get; init; }

    public required int Quantity { get; init; }

    public required PackageUnit PackageUnit { get; init; }

    public string? LotNumber { get; init; }

    /// <summary>Validade formatada, ou o aviso de que não foi identificada.</summary>
    public required string Validade { get; init; }

    public required bool ValidadeNaoIdentificada { get; init; }

    public required bool Vencido { get; init; }

    public required MedicationOrigin Origin { get; init; }

    public required MedicationEntryStatus Status { get; init; }

    public required string Recebimento { get; init; }

    public required bool PorLeitura { get; init; }

    public string? RejectionReason { get; init; }

    public static MedicationEntryListItemViewModel DeEntidade(MedicationEntry entrada, DateOnly hoje) => new()
    {
        Id = entrada.Id,
        MedicationId = entrada.MedicationId,
        NomeDoMedicamento = entrada.Medication?.GetDisplayName(),
        Quantity = entrada.Quantity,
        PackageUnit = entrada.Medication?.PackageUnit ?? PackageUnit.Unidade,
        LotNumber = entrada.LotNumber,
        Validade = entrada.ExpiryNotIdentified
            ? "Não identificada"
            : entrada.ExpiryDate?.ToString("dd/MM/yyyy") ?? "—",
        ValidadeNaoIdentificada = entrada.ExpiryNotIdentified,
        Vencido = entrada.IsExpiredOn(hoje),
        Origin = entrada.Origin,
        Status = entrada.Status,
        Recebimento = entrada.ReceivedOn.ToString("dd/MM/yyyy"),
        PorLeitura = entrada.IdentifiedByScan,
        RejectionReason = entrada.RejectionReason
    };
}

/// <summary>Tela de leitura de código de barras ou DataMatrix (US23).</summary>
public class ScanViewModel
{
    [Display(Name = "Código lido")]
    public string? Codigo { get; set; }
}
