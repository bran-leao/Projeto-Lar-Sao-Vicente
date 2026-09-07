using LarSaoVicente.Domain.Abstractions;
using LarSaoVicente.Infrastructure.Identity;
using LarSaoVicente.Infrastructure.Persistence;
using LarSaoVicente.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LarSaoVicente.Infrastructure;

/// <summary>
/// Registro dos serviços de infraestrutura no contêiner de injeção de dependências.
/// </summary>
/// <remarks>
/// Concentrar o registro aqui mantém o <c>Program.cs</c> enxuto e evita que a camada Web
/// precise conhecer detalhes do Entity Framework ou do Identity.
/// </remarks>
public static class DependencyInjection
{
    public const string ConnectionStringName = "DefaultConnection";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"A cadeia de conexão \"{ConnectionStringName}\" não foi configurada. " +
                "Verifique o arquivo appsettings.json.");

        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(
            connectionString,
            sqlServer => sqlServer.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.Configure<SeedOptions>(configuration.GetSection(SeedOptions.SectionName));
        services.AddScoped<DatabaseInitializer>();

        AddIdentity(services);

        return services;
    }

    /// <summary>
    /// Configura o ASP.NET Core Identity com as regras de senha e bloqueio da aplicação.
    /// </summary>
    /// <remarks>
    /// A interface padrão do Identity não é utilizada: ela oferece autocadastro,
    /// recuperação de senha por e-mail e login externo, que não fazem sentido em um
    /// sistema interno onde apenas a administração cria contas. As telas de login e
    /// logout são próprias, em português.
    /// </remarks>
    private static void AddIdentity(IServiceCollection services)
    {
        services
            .AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.User.RequireUniqueEmail = true;

                // Exigência mínima de senha. Comprimento é o fator que mais contribui
                // para a resistência a ataques de força bruta.
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;

                // Bloqueio temporário após tentativas seguidas de senha incorreta.
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();
    }
}
