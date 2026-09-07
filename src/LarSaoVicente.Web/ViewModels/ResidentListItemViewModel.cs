using LarSaoVicente.Domain.Enums;

namespace LarSaoVicente.Web.ViewModels;

/// <summary>Linha da listagem de residentes (US03).</summary>
public class ResidentListItemViewModel
{
    public required Guid Id { get; init; }

    public required string FullName { get; init; }

    /// <summary>Idade em anos completos, calculada na data da consulta.</summary>
    public required int Idade { get; init; }

    public required string Room { get; init; }

    public required DependencyLevel DependencyLevel { get; init; }

    public required ResidentStatus Status { get; init; }
}
