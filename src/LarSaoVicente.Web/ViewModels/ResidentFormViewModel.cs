using System.ComponentModel.DataAnnotations;
using LarSaoVicente.Domain.Entities;
using LarSaoVicente.Domain.Enums;

namespace LarSaoVicente.Web.ViewModels;

/// <summary>
/// Formulário de cadastro e de edição de residentes (US02 e US05).
/// </summary>
/// <remarks>
/// As telas trabalham com este ViewModel e nunca diretamente com a entidade
/// <see cref="Resident"/>. Assim, um campo enviado indevidamente no formulário — por
/// exemplo, as datas de auditoria — não tem como alcançar o banco de dados.
/// <para>
/// As anotações abaixo repetem as regras já garantidas pelo domínio, mas com uma
/// finalidade diferente: apresentar mensagens claras no formulário. As regras de datas,
/// que dependem do dia corrente, são validadas no controller.
/// </para>
/// </remarks>
public class ResidentFormViewModel
{
    public Guid? Id { get; set; }

    [Display(Name = "Nome completo")]
    [Required(ErrorMessage = "Informe o nome completo.")]
    [StringLength(Resident.FullNameMaxLength,
        ErrorMessage = "O nome completo deve ter no máximo {1} caracteres.")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Data de nascimento")]
    [Required(ErrorMessage = "Informe a data de nascimento.")]
    [DataType(DataType.Date)]
    public DateOnly? BirthDate { get; set; }

    [Display(Name = "Data de entrada na instituição")]
    [Required(ErrorMessage = "Informe a data de entrada na instituição.")]
    [DataType(DataType.Date)]
    public DateOnly? AdmissionDate { get; set; }

    [Display(Name = "Quarto")]
    [Required(ErrorMessage = "Informe o quarto.")]
    [StringLength(Resident.RoomMaxLength,
        ErrorMessage = "O quarto deve ter no máximo {1} caracteres.")]
    public string Room { get; set; } = string.Empty;

    [Display(Name = "Grau de dependência")]
    [Required(ErrorMessage = "Selecione o grau de dependência.")]
    public DependencyLevel? DependencyLevel { get; set; }

    [Display(Name = "Observações gerais")]
    [StringLength(Resident.NotesMaxLength,
        ErrorMessage = "As observações devem ter no máximo {1} caracteres.")]
    [DataType(DataType.MultilineText)]
    public string? Notes { get; set; }

    /// <summary>Indica se o formulário está editando um residente existente.</summary>
    public bool EhEdicao => Id.HasValue;

    public static ResidentFormViewModel DeEntidade(Resident residente) => new()
    {
        Id = residente.Id,
        FullName = residente.FullName,
        BirthDate = residente.BirthDate,
        AdmissionDate = residente.AdmissionDate,
        Room = residente.Room,
        DependencyLevel = residente.DependencyLevel,
        Notes = residente.Notes
    };
}
