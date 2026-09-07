using LarSaoVicente.Domain.Common;
using LarSaoVicente.Domain.Enums;

namespace LarSaoVicente.Domain.Entities;

/// <summary>
/// Residente atendido pela instituição.
/// </summary>
/// <remarks>
/// As propriedades possuem <c>private set</c> e a entidade só é alterada pelos métodos
/// públicos abaixo. Dessa forma as regras de negócio ficam concentradas no domínio e
/// não podem ser contornadas pelos controllers.
/// <para>
/// Nesta Sprint são registrados apenas os dados essenciais ao atendimento. Em respeito
/// ao princípio da minimização de dados da LGPD, CPF, RG e demais dados pessoais não
/// são coletados enquanto não houver necessidade concreta.
/// </para>
/// </remarks>
public class Resident : AuditableEntity
{
    public const int FullNameMaxLength = 150;
    public const int RoomMaxLength = 20;
    public const int NotesMaxLength = 1000;

    /// <summary>
    /// Identificador único do residente.
    /// </summary>
    /// <remarks>
    /// Utiliza <see cref="Guid"/> em vez de um inteiro sequencial para que as URLs do
    /// sistema não permitam descobrir quantos residentes existem nem percorrer os
    /// cadastros por tentativa e erro.
    /// </remarks>
    public Guid Id { get; private set; }

    public string FullName { get; private set; } = string.Empty;

    public DateOnly BirthDate { get; private set; }

    /// <summary>Data de entrada do residente na instituição.</summary>
    public DateOnly AdmissionDate { get; private set; }

    public string Room { get; private set; } = string.Empty;

    public DependencyLevel DependencyLevel { get; private set; }

    public ResidentStatus Status { get; private set; }

    /// <summary>Observações gerais de uso interno da equipe.</summary>
    public string? Notes { get; private set; }

    /// <summary>Construtor exigido pelo Entity Framework Core para materializar a entidade.</summary>
    private Resident()
    {
    }

    /// <summary>
    /// Cria um novo residente já validado e com situação <see cref="ResidentStatus.Ativo"/>.
    /// </summary>
    /// <param name="today">
    /// Data corrente no fuso da instituição, usada para validar as datas informadas.
    /// É recebida como parâmetro para manter o domínio livre de dependências externas.
    /// </param>
    /// <exception cref="DomainValidationException">Quando alguma regra de negócio é violada.</exception>
    public Resident(
        string fullName,
        DateOnly birthDate,
        DateOnly admissionDate,
        string room,
        DependencyLevel dependencyLevel,
        string? notes,
        DateOnly today)
    {
        Id = Guid.NewGuid();
        Status = ResidentStatus.Ativo;
        SetData(fullName, birthDate, admissionDate, room, dependencyLevel, notes, today);
    }

    /// <summary>
    /// Atualiza os dados cadastrais aplicando as mesmas validações do cadastro.
    /// </summary>
    /// <exception cref="DomainValidationException">Quando alguma regra de negócio é violada.</exception>
    public void Update(
        string fullName,
        DateOnly birthDate,
        DateOnly admissionDate,
        string room,
        DependencyLevel dependencyLevel,
        string? notes,
        DateOnly today)
    {
        SetData(fullName, birthDate, admissionDate, room, dependencyLevel, notes, today);
    }

    /// <summary>
    /// Inativa o residente, preservando todo o seu histórico.
    /// </summary>
    /// <remarks>Operação idempotente: inativar um residente já inativo não produz efeito.</remarks>
    public void Deactivate() => Status = ResidentStatus.Inativo;

    /// <summary>Reativa um residente anteriormente inativado.</summary>
    public void Reactivate() => Status = ResidentStatus.Ativo;

    /// <summary>
    /// Calcula a idade do residente, em anos completos, na data de referência informada.
    /// </summary>
    /// <remarks>
    /// A idade é sempre derivada da data de nascimento e nunca armazenada, para não
    /// existir um valor que se torne incorreto com a passagem do tempo.
    /// </remarks>
    public int GetAgeOn(DateOnly reference)
    {
        var age = reference.Year - BirthDate.Year;

        // Ainda não fez aniversário no ano de referência.
        if (reference < BirthDate.AddYears(age))
        {
            age--;
        }

        return age < 0 ? 0 : age;
    }

    private void SetData(
        string fullName,
        DateOnly birthDate,
        DateOnly admissionDate,
        string room,
        DependencyLevel dependencyLevel,
        string? notes,
        DateOnly today)
    {
        fullName = Normalize(fullName) ?? string.Empty;
        room = Normalize(room) ?? string.Empty;
        notes = Normalize(notes);

        if (fullName.Length == 0)
        {
            throw new DomainValidationException("O nome completo é obrigatório.");
        }

        if (fullName.Length > FullNameMaxLength)
        {
            throw new DomainValidationException($"O nome completo deve ter no máximo {FullNameMaxLength} caracteres.");
        }

        if (room.Length == 0)
        {
            throw new DomainValidationException("O quarto é obrigatório.");
        }

        if (room.Length > RoomMaxLength)
        {
            throw new DomainValidationException($"O quarto deve ter no máximo {RoomMaxLength} caracteres.");
        }

        if (birthDate > today)
        {
            throw new DomainValidationException("A data de nascimento não pode estar no futuro.");
        }

        if (admissionDate > today)
        {
            throw new DomainValidationException("A data de entrada não pode estar no futuro.");
        }

        if (admissionDate < birthDate)
        {
            throw new DomainValidationException("A data de entrada não pode ser anterior à data de nascimento.");
        }

        if (!Enum.IsDefined(dependencyLevel))
        {
            throw new DomainValidationException("O grau de dependência informado é inválido.");
        }

        if (notes is { Length: > NotesMaxLength })
        {
            throw new DomainValidationException($"As observações devem ter no máximo {NotesMaxLength} caracteres.");
        }

        FullName = fullName;
        BirthDate = birthDate;
        AdmissionDate = admissionDate;
        Room = room;
        DependencyLevel = dependencyLevel;
        Notes = notes;
    }

    /// <summary>Remove espaços nas extremidades e converte texto vazio em <c>null</c>.</summary>
    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}
