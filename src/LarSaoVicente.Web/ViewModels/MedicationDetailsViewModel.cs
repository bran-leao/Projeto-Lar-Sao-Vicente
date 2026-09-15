using LarSaoVicente.Domain.Abstractions;
using LarSaoVicente.Domain.Entities;
using LarSaoVicente.Domain.Enums;

namespace LarSaoVicente.Web.ViewModels;

/// <summary>Ficha de um item do catálogo, com as entradas registradas.</summary>
public class MedicationDetailsViewModel
{
    public required Guid Id { get; init; }

    public required string NomeExibicao { get; init; }

    public required string CommercialName { get; init; }

    public string? ActiveIngredient { get; init; }

    public string? Strength { get; init; }

    public PharmaceuticalForm? Form { get; init; }

    public int? UnitsPerPackage { get; init; }

    public required PackageUnit PackageUnit { get; init; }

    public string? Barcode { get; init; }

    public required MedicationCategory Category { get; init; }

    public required MedicationStatus Status { get; init; }

    public string? Notes { get; init; }

    public required string CriadoEm { get; init; }

    public string? AtualizadoEm { get; init; }

    public IReadOnlyList<MedicationEntryListItemViewModel> Entradas { get; init; } = [];

    /// <summary>Soma das entradas conferidas. É assim que o estoque existe: calculado.</summary>
    public int EmEstoque { get; init; }

    /// <summary>Quantas entradas ainda aguardam conferência.</summary>
    public int AguardandoConferencia { get; init; }

    public static MedicationDetailsViewModel DeEntidade(
        Medication item,
        IReadOnlyList<MedicationEntry> entradas,
        IDateTimeProvider relogio)
    {
        var hoje = relogio.Today;

        return new MedicationDetailsViewModel
        {
            Id = item.Id,
            NomeExibicao = item.GetDisplayName(),
            CommercialName = item.CommercialName,
            ActiveIngredient = item.ActiveIngredient,
            Strength = item.Strength,
            Form = item.Form,
            UnitsPerPackage = item.UnitsPerPackage,
            PackageUnit = item.PackageUnit,
            Barcode = item.Barcode,
            Category = item.Category,
            Status = item.Status,
            Notes = item.Notes,
            CriadoEm = relogio.ToInstitutionTime(item.CreatedAt).ToString("dd/MM/yyyy 'às' HH:mm"),
            AtualizadoEm = item.UpdatedAt is { } alterado
                ? relogio.ToInstitutionTime(alterado).ToString("dd/MM/yyyy 'às' HH:mm")
                : null,
            Entradas = entradas
                .Select(e => MedicationEntryListItemViewModel.DeEntidade(e, hoje))
                .ToList(),
            EmEstoque = entradas.Where(e => e.CountsTowardStock).Sum(e => e.Quantity),
            AguardandoConferencia = entradas.Count(e => e.Status == MedicationEntryStatus.AguardandoConferencia)
        };
    }
}
