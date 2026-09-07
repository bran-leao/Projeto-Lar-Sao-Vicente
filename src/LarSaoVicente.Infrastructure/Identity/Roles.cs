namespace LarSaoVicente.Infrastructure.Identity;

/// <summary>
/// Perfis de acesso do sistema.
/// </summary>
/// <remarks>
/// A Sprint 1 utiliza apenas <see cref="Administrador"/>, pois nenhuma regra de
/// diferenciação por perfil foi definida com a instituição até o momento: todo usuário
/// autenticado acessa as telas de residentes. A estrutura de perfis do Identity já fica
/// disponível para que as próximas Sprints (notadamente medicamentos e administração de
/// doses) restrinjam operações por função sem alteração estrutural.
/// </remarks>
public static class Roles
{
    public const string Administrador = "Administrador";

    /// <summary>Perfis criados automaticamente na inicialização do banco.</summary>
    public static readonly IReadOnlyList<string> Todos = [Administrador];
}
