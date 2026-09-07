using Microsoft.AspNetCore.Identity;

namespace LarSaoVicente.Infrastructure.Identity;

/// <summary>
/// Usuário do sistema (funcionário autorizado da instituição).
/// </summary>
/// <remarks>
/// Herda de <see cref="IdentityUser"/>, que já fornece o armazenamento da senha em
/// hash (PBKDF2 com salt), bloqueio por tentativas e o vínculo com perfis de acesso.
/// A senha nunca é armazenada em texto puro nem trafega de volta para a interface.
/// <para>
/// Nesta Sprint apenas o nome de exibição é acrescentado. O cadastro completo de
/// usuários é escopo do módulo "Usuários", previsto para uma Sprint futura.
/// </para>
/// </remarks>
public class ApplicationUser : IdentityUser
{
    public const int FullNameMaxLength = 150;

    /// <summary>Nome do funcionário, exibido no cabeçalho e nos registros de auditoria.</summary>
    public string FullName { get; set; } = string.Empty;
}
