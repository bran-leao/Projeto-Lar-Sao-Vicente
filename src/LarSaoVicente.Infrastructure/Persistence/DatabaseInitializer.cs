using LarSaoVicente.Domain.Abstractions;
using LarSaoVicente.Domain.Entities;
using LarSaoVicente.Domain.Enums;
using LarSaoVicente.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LarSaoVicente.Infrastructure.Persistence;

/// <summary>
/// Prepara o banco de dados na inicialização da aplicação: aplica as migrations
/// pendentes, cria os perfis de acesso, o usuário administrador e, quando habilitado,
/// os dados de demonstração.
/// </summary>
public class DatabaseInitializer
{
    private readonly AppDbContext _context;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly SeedOptions _options;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(
        AppDbContext context,
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        IDateTimeProvider dateTimeProvider,
        IOptions<SeedOptions> options,
        ILogger<DatabaseInitializer> logger)
    {
        _context = context;
        _roleManager = roleManager;
        _userManager = userManager;
        _dateTimeProvider = dateTimeProvider;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>Aplica as migrations pendentes ao banco configurado.</summary>
    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        await _context.Database.MigrateAsync(cancellationToken);
        _logger.LogInformation("Migrations aplicadas com sucesso.");
    }

    /// <summary>
    /// Cria os registros mínimos para o sistema ser utilizável.
    /// </summary>
    /// <remarks>Todas as etapas são idempotentes: executar novamente não duplica dados.</remarks>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedRolesAsync();
        await SeedAdministratorAsync();
        await SeedDemoResidentsAsync(cancellationToken);
    }

    private async Task SeedRolesAsync()
    {
        foreach (var perfil in Roles.Todos)
        {
            if (await _roleManager.RoleExistsAsync(perfil))
            {
                continue;
            }

            var resultado = await _roleManager.CreateAsync(new IdentityRole(perfil));
            if (resultado.Succeeded)
            {
                _logger.LogInformation("Perfil de acesso {Perfil} criado.", perfil);
            }
            else
            {
                _logger.LogError(
                    "Falha ao criar o perfil {Perfil}: {Erros}",
                    perfil,
                    string.Join("; ", resultado.Errors.Select(e => e.Description)));
            }
        }
    }

    private async Task SeedAdministratorAsync()
    {
        var administrador = _options.Administrator;

        // Sem credenciais configuradas nada é criado: um usuário com senha padrão
        // conhecida seria uma porta aberta no ambiente da instituição.
        if (string.IsNullOrWhiteSpace(administrador.Email) || string.IsNullOrWhiteSpace(administrador.Password))
        {
            _logger.LogWarning(
                "Nenhum administrador inicial foi configurado na seção \"{Secao}\". " +
                "Configure o e-mail e a senha para conseguir acessar o sistema.",
                SeedOptions.SectionName);
            return;
        }

        if (await _userManager.FindByEmailAsync(administrador.Email) is not null)
        {
            return;
        }

        var usuario = new ApplicationUser
        {
            UserName = administrador.Email,
            Email = administrador.Email,
            EmailConfirmed = true,
            FullName = string.IsNullOrWhiteSpace(administrador.FullName)
                ? administrador.Email
                : administrador.FullName
        };

        // A senha é convertida em hash pelo Identity; o texto puro nunca chega ao banco.
        var resultado = await _userManager.CreateAsync(usuario, administrador.Password);
        if (!resultado.Succeeded)
        {
            _logger.LogError(
                "Falha ao criar o administrador inicial: {Erros}",
                string.Join("; ", resultado.Errors.Select(e => e.Description)));
            return;
        }

        await _userManager.AddToRoleAsync(usuario, Roles.Administrador);
        _logger.LogInformation("Administrador inicial {Email} criado.", administrador.Email);
    }

    /// <summary>
    /// Cria residentes fictícios para demonstração da Sprint.
    /// </summary>
    /// <remarks>
    /// Os nomes e as informações são inventados. Nenhum dado real de residentes do
    /// Lar São Vicente de Paulo é utilizado, em respeito à LGPD e ao sigilo das pessoas
    /// atendidas. Os registros são identificados nas observações como dados de demonstração.
    /// </remarks>
    private async Task SeedDemoResidentsAsync(CancellationToken cancellationToken)
    {
        if (!_options.DemoData)
        {
            return;
        }

        // Só popula um banco vazio, para nunca misturar dados fictícios com dados reais.
        if (await _context.Residents.AnyAsync(cancellationToken))
        {
            return;
        }

        const string marcacao = "Registro fictício, criado apenas para demonstração do sistema.";
        var hoje = _dateTimeProvider.Today;

        var residentes = new[]
        {
            new Resident("Maria Oliveira", new DateOnly(1940, 4, 12), new DateOnly(2019, 8, 3), "101",
                DependencyLevel.GrauI, marcacao, hoje),
            new Resident("Antônio Souza", new DateOnly(1936, 11, 27), new DateOnly(2021, 2, 17), "102",
                DependencyLevel.GrauII, $"{marcacao} Utiliza andador.", hoje),
            new Resident("José Ferreira", new DateOnly(1944, 1, 9), new DateOnly(2022, 5, 30), "205",
                DependencyLevel.GrauIII, $"{marcacao} Acamado, requer auxílio integral.", hoje),
            new Resident("Terezinha Ramos", new DateOnly(1948, 7, 21), new DateOnly(2023, 9, 12), "206",
                DependencyLevel.GrauII, marcacao, hoje),
            new Resident("Sebastião Lima", new DateOnly(1939, 2, 5), new DateOnly(2018, 3, 22), "103",
                DependencyLevel.GrauI, marcacao, hoje)
        };

        // Um residente inativo permite demonstrar que a inativação preserva o histórico.
        residentes[^1].Deactivate();

        _context.Residents.AddRange(residentes);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("{Quantidade} residentes de demonstração criados.", residentes.Length);
    }
}
