namespace LarSaoVicente.Domain.Abstractions;

/// <summary>
/// Identifica o usuário autenticado responsável pela operação em andamento.
/// </summary>
/// <remarks>
/// Utilizado pelo preenchimento automático dos campos de auditoria. Nas próximas
/// Sprints, é esta mesma abstração que registrará o responsável por cada
/// movimentação de medicamento.
/// </remarks>
public interface ICurrentUser
{
    /// <summary>Identificador do usuário autenticado, ou <c>null</c> quando não há sessão ativa.</summary>
    string? UserId { get; }
}
