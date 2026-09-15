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
        await SeedDemoCatalogAsync(cancellationToken);
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

    /// <summary>
    /// Cria um catálogo mínimo de demonstração, com entradas em situações diferentes.
    /// </summary>
    /// <remarks>
    /// Os itens escolhidos não são aleatórios: reproduzem as situações que a instituição
    /// realmente enfrenta e que a Sprint Review precisa mostrar — o mesmo remédio com
    /// nome comercial e princípio ativo diferentes, o item recebido em envelopes, o
    /// insumo sem princípio ativo e a doação com validade ilegível.
    /// <para>
    /// Nomes de medicamento são informação pública de embalagem. Nenhum dado de residente
    /// da instituição é utilizado, aqui ou em qualquer outro lugar do sistema.
    /// </para>
    /// </remarks>
    private async Task SeedDemoCatalogAsync(CancellationToken cancellationToken)
    {
        if (!_options.DemoData)
        {
            return;
        }

        // Só popula um catálogo vazio, para nunca misturar demonstração com dado real.
        if (await _context.Medications.AnyAsync(cancellationToken))
        {
            return;
        }

        const string marcacao = "Registro fictício, criado apenas para demonstração do sistema.";
        var hoje = _dateTimeProvider.Today;

        // O mesmo medicamento pelo nome comercial: nas planilhas da instituição ele
        // aparece ora assim, ora como Nitrofurantoína, sem nada ligando os dois registros.
        var macrodantina = new Medication(
            "Macrodantina", "Nitrofurantoína", "100 mg", PharmaceuticalForm.Capsula,
            28, PackageUnit.Caixa, "7899990000113", MedicationCategory.Medicamento, marcacao);

        var losartana = new Medication(
            "Losartana", "Losartana potássica", "50 mg", PharmaceuticalForm.Comprimido,
            30, PackageUnit.Caixa, "7891000315507", MedicationCategory.Medicamento, marcacao);

        // Recebido em envelopes: é o caso que motivou a quantidade ficar na entrada.
        var dipirona = new Medication(
            "Dipirona", "Dipirona sódica", "500 mg", PharmaceuticalForm.Sache,
            1, PackageUnit.Envelope, "7899990000281", MedicationCategory.Medicamento, marcacao);

        var insulina = new Medication(
            "Insulina NPH", "Insulina humana NPH", "100 UI/mL", PharmaceuticalForm.Injetavel,
            1, PackageUnit.Frasco, null, MedicationCategory.Medicamento,
            $"{marcacao} Conservar sob refrigeração.");

        // Insumo: não tem princípio ativo nem forma farmacêutica, e é assim mesmo.
        var luvas = new Medication(
            "Luva de procedimento M", null, null, null,
            100, PackageUnit.Caixa, "7899990000359", MedicationCategory.Insumo, marcacao);

        var descontinuado = new Medication(
            "Xarope descontinuado", "Guaifenesina", "100 mg/mL", PharmaceuticalForm.SolucaoOral,
            1, PackageUnit.Frasco, "7899990000427", MedicationCategory.Medicamento,
            $"{marcacao} Item inativado para demonstrar que o histórico é preservado.");

        descontinuado.Deactivate();

        var itens = new[] { macrodantina, losartana, dipirona, insulina, luvas, descontinuado };
        _context.Medications.AddRange(itens);

        var entradas = new List<MedicationEntry>
        {
            // Conferida: é o que forma o estoque.
            new(losartana.Id, 12, "LOT2026A", hoje.AddMonths(14), false,
                MedicationOrigin.Distribuidora, hoje.AddDays(-20), true, "SN00012345",
                marcacao, hoje),

            // Onze envelopes em um registro só, como a enfermagem pediu.
            new(dipirona.Id, 11, "DIP7788", hoje.AddMonths(9), false,
                MedicationOrigin.Familia, hoje.AddDays(-2), false, null,
                $"{marcacao} Trazidos pela família em envelopes avulsos.", hoje),

            // Doação sem validade legível: o caso mais frequente, e o de maior risco.
            new(macrodantina.Id, 3, null, null, true,
                MedicationOrigin.Doacao, hoje.AddDays(-1), false, null,
                $"{marcacao} Cartela avulsa, embalagem sem validade legível.", hoje),

            // Lote já vencido: precisa ser registrado para poder ser recusado.
            new(insulina.Id, 2, "INS0001", hoje.AddDays(-30), false,
                MedicationOrigin.Doacao, hoje.AddDays(-3), false, null,
                $"{marcacao} Chegou com o lote já vencido.", hoje)
        };

        // Só a primeira é liberada: as demais ficam na tela de conferência, que é
        // justamente o que a Review precisa mostrar.
        entradas[0].Confirm(reviewedByUserId: null, _dateTimeProvider.UtcNow);

        _context.MedicationEntries.AddRange(entradas);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Catálogo de demonstração criado: {Itens} itens e {Entradas} entradas.",
            itens.Length, entradas.Count);
    }
}
