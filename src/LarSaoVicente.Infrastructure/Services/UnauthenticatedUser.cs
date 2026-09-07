using LarSaoVicente.Domain.Abstractions;

namespace LarSaoVicente.Infrastructure.Services;

/// <summary>
/// <see cref="ICurrentUser"/> sem usuário autenticado.
/// </summary>
/// <remarks>
/// Utilizado em contextos que não possuem requisição HTTP, como a criação de migrations
/// em tempo de design e a carga inicial de dados executada na inicialização.
/// Nesses casos os campos de auditoria ficam sem responsável, o que é correto: a operação
/// não partiu de um funcionário.
/// </remarks>
public class UnauthenticatedUser : ICurrentUser
{
    public string? UserId => null;
}
