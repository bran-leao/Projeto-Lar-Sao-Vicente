using LarSaoVicente.Domain.Abstractions;
using LarSaoVicente.Domain.Entities;
using LarSaoVicente.Domain.Enums;

namespace LarSaoVicente.Web.ViewModels;

/// <summary>
/// Perfil do residente (US04).
/// </summary>
/// <remarks>
/// A tela é organizada em seções para receber, nas próximas Sprints, os blocos de
/// medicamentos, agenda e ocorrências previstos no backlog — sem que sua estrutura
/// precise ser refeita.
/// </remarks>
public class ResidentDetailsViewModel
{
    public required Guid Id { get; init; }

    public required string FullName { get; init; }

    public required DateOnly BirthDate { get; init; }

    public required int Idade { get; init; }

    public required DateOnly AdmissionDate { get; init; }

    public required string Room { get; init; }

    public required DependencyLevel DependencyLevel { get; init; }

    public required ResidentStatus Status { get; init; }

    public string? Notes { get; init; }

    /// <summary>Momento do cadastro, já convertido para o horário local de exibição.</summary>
    public required DateTime CriadoEm { get; init; }

    /// <summary>Momento da última alteração, ou nulo se o cadastro nunca foi editado.</summary>
    public DateTime? AtualizadoEm { get; init; }

    public bool EstaAtivo => Status == ResidentStatus.Ativo;

    public static ResidentDetailsViewModel DeEntidade(Resident residente, IDateTimeProvider relogio) => new()
    {
        Id = residente.Id,
        FullName = residente.FullName,
        BirthDate = residente.BirthDate,
        Idade = residente.GetAgeOn(relogio.Today),
        AdmissionDate = residente.AdmissionDate,
        Room = residente.Room,
        DependencyLevel = residente.DependencyLevel,
        Status = residente.Status,
        Notes = residente.Notes,
        CriadoEm = relogio.ToInstitutionTime(residente.CreatedAt),
        AtualizadoEm = residente.UpdatedAt.HasValue
            ? relogio.ToInstitutionTime(residente.UpdatedAt.Value)
            : null
    };
}
