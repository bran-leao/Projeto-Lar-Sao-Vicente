using System.Net;
using System.Text.RegularExpressions;

namespace LarSaoVicente.Tests.Infraestrutura;

/// <summary>
/// Auxílios para exercitar a aplicação por HTTP, do mesmo modo que um navegador faria.
/// </summary>
/// <remarks>
/// Os formulários do sistema exigem token antifalsificação. Os métodos abaixo leem o
/// token da página antes de enviar cada requisição, replicando o comportamento real do
/// navegador em vez de desativar a proteção durante os testes.
/// </remarks>
public static class ClienteHttp
{
    private static readonly Regex TokenAntifalsificacao = new(
        """<input name="__RequestVerificationToken" type="hidden" value="([^"]+)" />""",
        RegexOptions.Compiled);

    /// <summary>Cria um cliente que preserva cookies e não segue redirecionamentos.</summary>
    /// <remarks>
    /// Não seguir os redirecionamentos permite verificar para onde a aplicação está
    /// enviando o usuário, que é justamente o que os testes de proteção de rota avaliam.
    /// </remarks>
    public static HttpClient CriarCliente(this AplicacaoDeTeste aplicacao) =>
        aplicacao.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

    /// <summary>Obtém o token antifalsificação presente no formulário da página informada.</summary>
    public static async Task<string> ObterTokenAsync(this HttpClient cliente, string url)
    {
        var html = await cliente.GetStringAsync(url);
        var correspondencia = TokenAntifalsificacao.Match(html);

        Assert.True(correspondencia.Success, $"Nenhum token antifalsificação encontrado em {url}.");
        return correspondencia.Groups[1].Value;
    }

    /// <summary>Envia um formulário, incluindo automaticamente o token antifalsificação.</summary>
    public static async Task<HttpResponseMessage> EnviarFormularioAsync(
        this HttpClient cliente,
        string urlDoFormulario,
        string urlDeEnvio,
        Dictionary<string, string> campos)
    {
        var token = await cliente.ObterTokenAsync(urlDoFormulario);

        var conteudo = new Dictionary<string, string>(campos)
        {
            ["__RequestVerificationToken"] = token
        };

        return await cliente.PostAsync(urlDeEnvio, new FormUrlEncodedContent(conteudo));
    }

    /// <summary>Autentica o cliente com o usuário de teste.</summary>
    public static async Task AutenticarAsync(this HttpClient cliente)
    {
        var resposta = await cliente.EnviarFormularioAsync("/Conta/Login", "/Conta/Login",
            new Dictionary<string, string>
            {
                ["Email"] = AplicacaoDeTeste.EmailUsuario,
                ["Password"] = AplicacaoDeTeste.SenhaUsuario
            });

        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
    }
}
