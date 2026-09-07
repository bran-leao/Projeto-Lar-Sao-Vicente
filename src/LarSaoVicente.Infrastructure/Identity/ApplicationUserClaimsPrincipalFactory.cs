using System.Security.Claims;
using LarSaoVicente.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace LarSaoVicente.Infrastructure.Identity;

/// <summary>
/// Acrescenta o nome do funcionário aos dados da sessão autenticada.
/// </summary>
/// <remarks>
/// Sem isso, exibir "Olá, Fulano" no cabeçalho exigiria uma consulta ao banco a cada
/// requisição. Guardando o nome como claim, ele acompanha o cookie de autenticação.
/// Apenas o nome é incluído: o cookie não deve carregar informações desnecessárias.
/// </remarks>
public class ApplicationUserClaimsPrincipalFactory
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
{
    /// <summary>Nome da claim que armazena o nome de exibição do funcionário.</summary>
    public const string FullNameClaimType = "nome_completo";

    public ApplicationUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> options)
        : base(userManager, roleManager, options)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identidade = await base.GenerateClaimsAsync(user);

        if (!string.IsNullOrWhiteSpace(user.FullName))
        {
            identidade.AddClaim(new Claim(FullNameClaimType, user.FullName));
        }

        return identidade;
    }
}
