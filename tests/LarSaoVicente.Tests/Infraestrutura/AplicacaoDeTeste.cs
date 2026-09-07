using System.Data.Common;
using LarSaoVicente.Infrastructure.Identity;
using LarSaoVicente.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace LarSaoVicente.Tests.Infraestrutura;

/// <summary>
/// Hospeda a aplicação real em memória para os testes de integração.
/// </summary>
/// <remarks>
/// O banco SQL Server é substituído por um SQLite em memória. Assim a suíte roda em
/// qualquer máquina, sem depender de um servidor instalado, e cada classe de teste
/// trabalha em um banco próprio e descartável.
/// <para>
/// Tudo o mais permanece igual à aplicação em produção — o mesmo pipeline, as mesmas
/// políticas de autorização e as mesmas telas — de modo que os testes exercitam o
/// comportamento real do sistema.
/// </para>
/// </remarks>
public class AplicacaoDeTeste : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string EmailUsuario = "funcionario@teste.local";
    public const string SenhaUsuario = "Teste@2026";
    public const string NomeUsuario = "Funcionário de Teste";

    /// <summary>
    /// A conexão precisa continuar aberta: um banco SQLite em memória deixa de existir
    /// assim que a última conexão com ele é encerrada.
    /// </summary>
    private readonly DbConnection _conexao = new SqliteConnection("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configuracao) =>
        {
            configuracao.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // A cadeia de conexão é exigida na inicialização, mas não chega a ser
                // utilizada: o provedor é substituído logo abaixo.
                ["ConnectionStrings:DefaultConnection"] = "Server=(nao-utilizado);Database=Testes",

                // Os testes preparam o próprio banco, sem migrations nem dados de demonstração.
                ["Database:AutoMigrate"] = "false",
                ["Seed:DemoData"] = "false",
                ["Seed:Administrator:Email"] = string.Empty,
                ["Seed:Administrator:Password"] = string.Empty
            });
        });

        builder.ConfigureServices(services =>
        {
            // A partir do EF Core 9 o AddDbContext registra a configuração das opções
            // como um serviço. Remover apenas o DbContextOptions deixaria a configuração
            // do SQL Server registrada, e os dois provedores seriam aplicados ao mesmo
            // contexto — o que o Entity Framework recusa.
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<AppDbContext>();

            _conexao.Open();
            services.AddDbContext<AppDbContext>(opcoes => opcoes.UseSqlite(_conexao));
        });
    }

    public async Task InitializeAsync()
    {
        using var escopo = Services.CreateScope();
        var provedor = escopo.ServiceProvider;

        var contexto = provedor.GetRequiredService<AppDbContext>();
        await contexto.Database.EnsureCreatedAsync();

        var gerenciadorDePerfis = provedor.GetRequiredService<RoleManager<IdentityRole>>();
        await gerenciadorDePerfis.CreateAsync(new IdentityRole(Roles.Administrador));

        var gerenciadorDeUsuarios = provedor.GetRequiredService<UserManager<ApplicationUser>>();
        var usuario = new ApplicationUser
        {
            UserName = EmailUsuario,
            Email = EmailUsuario,
            EmailConfirmed = true,
            FullName = NomeUsuario
        };

        var resultado = await gerenciadorDeUsuarios.CreateAsync(usuario, SenhaUsuario);
        Assert.True(resultado.Succeeded,
            "Falha ao criar o usuário de teste: " +
            string.Join("; ", resultado.Errors.Select(e => e.Description)));

        await gerenciadorDeUsuarios.AddToRoleAsync(usuario, Roles.Administrador);
    }

    /// <summary>Executa uma consulta ao banco fora da aplicação, para conferir o que foi gravado.</summary>
    public async Task<T> ConsultarBancoAsync<T>(Func<AppDbContext, Task<T>> consulta)
    {
        using var escopo = Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<AppDbContext>();
        return await consulta(contexto);
    }

    public override async ValueTask DisposeAsync()
    {
        // Encerrar a conexão descarta o banco em memória associado a ela.
        await _conexao.DisposeAsync();
        await base.DisposeAsync();
    }

    Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();
}
