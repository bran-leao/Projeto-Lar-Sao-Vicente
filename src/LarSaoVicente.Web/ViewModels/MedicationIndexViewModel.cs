using LarSaoVicente.Domain.Enums;

namespace LarSaoVicente.Web.ViewModels;

/// <summary>Listagem do catálogo com pesquisa por nome ou princípio ativo (US24).</summary>
public class MedicationIndexViewModel
{
    public string? Busca { get; init; }

    public IReadOnlyList<MedicationListItemViewModel> Itens { get; init; } = [];

    /// <summary>Verdadeiro quando o catálogo está completamente vazio.</summary>
    public bool SemCadastros { get; init; }

    /// <summary>Verdadeiro quando existem itens, mas nenhum corresponde à pesquisa.</summary>
    public bool BuscaSemResultado => Itens.Count == 0 && !SemCadastros;
}

/// <summary>Uma linha da listagem do catálogo.</summary>
public class MedicationListItemViewModel
{
    public required Guid Id { get; init; }

    public required string NomeExibicao { get; init; }

    public string? ActiveIngredient { get; init; }

    public required PackageUnit PackageUnit { get; init; }

    public int? UnitsPerPackage { get; init; }

    public required MedicationCategory Category { get; init; }

    public required MedicationStatus Status { get; init; }

    public bool TemCodigoDeBarras { get; init; }

    /// <summary>Quantidade em estoque: soma das entradas conferidas.</summary>
    public int EmEstoque { get; init; }
}
