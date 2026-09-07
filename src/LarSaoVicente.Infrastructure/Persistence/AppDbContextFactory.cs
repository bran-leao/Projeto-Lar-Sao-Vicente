using LarSaoVicente.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LarSaoVicente.Infrastructure.Persistence;

/// <summary>
/// Cria o <see cref="AppDbContext"/> para as ferramentas de linha de comando do
/// Entity Framework (<c>dotnet ef migrations</c>, <c>dotnet ef database update</c>).
/// </summary>
/// <remarks>
/// Sem esta fábrica, a ferramenta precisaria inicializar toda a aplicação web apenas
/// para descobrir como construir o contexto.
/// <para>
/// A cadeia de conexão pode ser informada pela variável de ambiente
/// <c>LARSAOVICENTE_CONNECTION</c>. Quando ausente, é usado um valor local padrão:
/// gerar uma migration não exige um banco acessível, apenas o provedor correto.
/// </para>
/// </remarks>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string ConnectionEnvironmentVariable = "LARSAOVICENTE_CONNECTION";

    private const string FallbackConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=LarSaoVicente;Trusted_Connection=True;TrustServerCertificate=True";

    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable(ConnectionEnvironmentVariable) ?? FallbackConnectionString;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        // Em tempo de design não há requisição HTTP nem usuário autenticado.
        return new AppDbContext(options, new SystemDateTimeProvider(), new UnauthenticatedUser());
    }
}
