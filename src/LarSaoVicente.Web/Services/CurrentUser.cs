using System.Security.Claims;
using LarSaoVicente.Domain.Abstractions;

namespace LarSaoVicente.Web.Services;

/// <summary>
/// Obtém o usuário autenticado a partir da requisição HTTP em andamento.
/// </summary>
/// <remarks>
/// Implementado na camada Web porque o <see cref="IHttpContextAccessor"/> é um detalhe
/// de apresentação: o domínio e a infraestrutura conhecem apenas a interface
/// <see cref="ICurrentUser"/>.
/// </remarks>
public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId =>
        _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
}
