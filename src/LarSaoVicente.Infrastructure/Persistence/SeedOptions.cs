namespace LarSaoVicente.Infrastructure.Persistence;

/// <summary>
/// Configuração da carga inicial de dados, lida da seção "Seed" do appsettings.
/// </summary>
public class SeedOptions
{
    public const string SectionName = "Seed";

    /// <summary>
    /// Quando verdadeiro, cria alguns residentes fictícios para demonstração.
    /// </summary>
    /// <remarks>
    /// Deve permanecer desligado em produção: a instituição usará dados reais, e registros
    /// de demonstração misturados aos verdadeiros distorceriam os indicadores do Dashboard.
    /// </remarks>
    public bool DemoData { get; set; }

    /// <summary>Usuário administrador criado na primeira execução.</summary>
    public SeedAdministratorOptions Administrator { get; set; } = new();
}

/// <summary>
/// Credenciais do administrador inicial.
/// </summary>
/// <remarks>
/// A senha não fica no código-fonte. Em desenvolvimento ela é lida do
/// appsettings.Development.json; em produção deve ser fornecida por variável de ambiente
/// ou pelo User Secrets e alterada no primeiro acesso.
/// </remarks>
public class SeedAdministratorOptions
{
    public string Email { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
