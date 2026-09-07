using System.Globalization;
using LarSaoVicente.Domain.Abstractions;
using LarSaoVicente.Infrastructure;
using LarSaoVicente.Infrastructure.Persistence;
using LarSaoVicente.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Cultura da aplicação
// ---------------------------------------------------------------------------
// A instituição é brasileira: datas são exibidas como dd/MM/yyyy e números usam
// vírgula decimal. A cultura é fixada para não variar conforme o servidor.
var culturaBrasileira = new CultureInfo("pt-BR");
CultureInfo.DefaultThreadCurrentCulture = culturaBrasileira;
CultureInfo.DefaultThreadCurrentUICulture = culturaBrasileira;

// ---------------------------------------------------------------------------
// Serviços
// ---------------------------------------------------------------------------
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Conta/Login";
    options.LogoutPath = "/Conta/Sair";
    options.AccessDeniedPath = "/Conta/AcessoNegado";

    // Uma jornada de trabalho. Encerrada a sessão, é preciso autenticar novamente.
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;

    // O cookie não pode ser lido por JavaScript, o que reduz o impacto de um XSS.
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;

    // Em produção o cookie só trafega por HTTPS. Em desenvolvimento a exigência é
    // relaxada para permitir a execução local em HTTP.
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});

builder.Services.AddAuthorization(options =>
{
    // Toda página exige autenticação por padrão. Para liberar o acesso anônimo é
    // preciso marcar explicitamente com [AllowAnonymous], evitando que uma tela nova
    // fique desprotegida por esquecimento.
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddControllersWithViews(options =>
{
    // Valida o token antifalsificação em toda requisição que altera dados,
    // protegendo os formulários contra CSRF.
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

var app = builder.Build();

// ---------------------------------------------------------------------------
// Pipeline
// ---------------------------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Erro");
    app.UseStatusCodePagesWithReExecute("/Erro/{0}");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Os arquivos estáticos precisam ser liberados explicitamente: a política padrão de
// autorização se aplica a todos os endpoints, inclusive aos criados por MapStaticAssets.
// Sem isto, o CSS e o JavaScript da própria tela de login seriam bloqueados e ela
// apareceria sem formatação para quem ainda não se autenticou.
app.MapStaticAssets().AllowAnonymous();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

// Aplica as migrations pendentes e cria os dados iniciais.
//
// Executar as migrations na inicializacao simplifica a implantacao em um projeto deste
// porte. Se a etapa falhar, a aplicacao encerra em vez de subir com o banco incompleto:
// um erro visivel e preferivel a um sistema que aparenta funcionar e grava dados errados.
//
// Os testes de integracao desligam a etapa por configuracao, pois criam o proprio banco.
if (app.Configuration.GetValue("Database:AutoMigrate", defaultValue: true))
{
    using var escopo = app.Services.CreateScope();
    var inicializador = escopo.ServiceProvider.GetRequiredService<DatabaseInitializer>();

    await inicializador.MigrateAsync();
    await inicializador.SeedAsync();
}

app.Run();

/// <summary>
/// Tornada pública para que o projeto de testes possa hospedar a aplicação
/// em memória nos testes de integração.
/// </summary>
public partial class Program;
