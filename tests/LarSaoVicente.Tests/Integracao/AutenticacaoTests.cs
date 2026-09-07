using System.Net;
using LarSaoVicente.Tests.Infraestrutura;

namespace LarSaoVicente.Tests.Integracao;

/// <summary>Testes de integração da autenticação (US01).</summary>
public class AutenticacaoTests : IClassFixture<AplicacaoDeTeste>
{
    private readonly AplicacaoDeTeste _aplicacao;

    public AutenticacaoTests(AplicacaoDeTeste aplicacao)
    {
        _aplicacao = aplicacao;
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/Residentes")]
    [InlineData("/Residentes/Novo")]
    public async Task PaginasProtegidas_SemAutenticacao_RedirecionamParaLogin(string url)
    {
        var cliente = _aplicacao.CriarCliente();

        var resposta = await cliente.GetAsync(url);

        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        Assert.Equal("/Conta/Login", CaminhoDe(resposta.Headers.Location));
    }

    /// <summary>
    /// Extrai apenas o caminho do destino do redirecionamento.
    /// </summary>
    /// <remarks>
    /// O desafio de autenticação responde com um endereço absoluto, enquanto os
    /// redirecionamentos gerados pelos controllers são relativos. Comparar somente o
    /// caminho torna a verificação independente dessa diferença.
    /// </remarks>
    private static string CaminhoDe(Uri? destino)
    {
        Assert.NotNull(destino);

        return destino.IsAbsoluteUri
            ? destino.AbsolutePath
            : destino.OriginalString.Split('?')[0];
    }

    [Fact]
    public async Task PaginaProtegida_SemAutenticacao_PreservaODestinoPretendido()
    {
        var cliente = _aplicacao.CriarCliente();

        var resposta = await cliente.GetAsync("/Residentes/Novo");

        // Após entrar, o funcionário deve ser levado à página que tentou abrir.
        Assert.Contains("ReturnUrl=%2FResidentes%2FNovo", resposta.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task TelaDeLogin_EstaAcessivelSemAutenticacao()
    {
        var cliente = _aplicacao.CriarCliente();

        var resposta = await cliente.GetAsync("/Conta/Login");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    /// <summary>
    /// A política padrão de autorização se aplica a todos os endpoints, inclusive aos
    /// arquivos estáticos. Sem a liberação explícita, a própria tela de login seria
    /// exibida sem estilo e sem JavaScript.
    /// </summary>
    [Theory]
    [InlineData("/css/site.css")]
    [InlineData("/lib/bootstrap/dist/css/bootstrap.min.css")]
    [InlineData("/lib/jquery/dist/jquery.min.js")]
    public async Task ArquivosEstaticos_EstaoAcessiveisSemAutenticacao(string url)
    {
        var cliente = _aplicacao.CriarCliente();

        var resposta = await cliente.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task Login_ComCredenciaisValidas_AutenticaERedirecionaParaODashboard()
    {
        var cliente = _aplicacao.CriarCliente();

        var resposta = await cliente.EnviarFormularioAsync("/Conta/Login", "/Conta/Login",
            new Dictionary<string, string>
            {
                ["Email"] = AplicacaoDeTeste.EmailUsuario,
                ["Password"] = AplicacaoDeTeste.SenhaUsuario
            });

        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        Assert.Equal("/", resposta.Headers.Location?.OriginalString);

        // O cookie recebido deve realmente dar acesso às páginas protegidas.
        var dashboard = await cliente.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, dashboard.StatusCode);
    }

    [Fact]
    public async Task Login_ComSenhaIncorreta_ApresentaMensagemENaoAutentica()
    {
        var cliente = _aplicacao.CriarCliente();

        var resposta = await cliente.EnviarFormularioAsync("/Conta/Login", "/Conta/Login",
            new Dictionary<string, string>
            {
                ["Email"] = AplicacaoDeTeste.EmailUsuario,
                ["Password"] = "SenhaIncorreta1"
            });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var html = await resposta.Content.ReadAsStringAsync();
        Assert.Contains("senha inv", html, StringComparison.OrdinalIgnoreCase);

        var dashboard = await cliente.GetAsync("/");
        Assert.Equal(HttpStatusCode.Redirect, dashboard.StatusCode);
    }

    [Fact]
    public async Task Login_ComEmailInexistente_NaoRevelaQueOEmailNaoExiste()
    {
        var cliente = _aplicacao.CriarCliente();

        var resposta = await cliente.EnviarFormularioAsync("/Conta/Login", "/Conta/Login",
            new Dictionary<string, string>
            {
                ["Email"] = "desconhecido@teste.local",
                ["Password"] = "QualquerSenha1"
            });

        var html = await resposta.Content.ReadAsStringAsync();

        // A mensagem precisa ser a mesma de senha incorreta, para não permitir
        // descobrir quais e-mails possuem conta no sistema.
        Assert.Contains("senha inv", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("não encontrado", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("não existe", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Logout_EncerraASessaoEBloqueiaAsPaginasProtegidas()
    {
        var cliente = _aplicacao.CriarCliente();
        await cliente.AutenticarAsync();
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/Residentes")).StatusCode);

        var saida = await cliente.EnviarFormularioAsync("/Residentes", "/Conta/Sair", []);

        Assert.Equal(HttpStatusCode.Redirect, saida.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await cliente.GetAsync("/Residentes")).StatusCode);
    }

    /// <summary>
    /// O logout é exposto apenas por POST: fosse um link comum, bastaria um site externo
    /// carregar essa URL para desconectar o funcionário sem que ele pedisse.
    /// </summary>
    [Fact]
    public async Task Logout_NaoAceitaRequisicaoGet()
    {
        var cliente = _aplicacao.CriarCliente();
        await cliente.AutenticarAsync();

        var resposta = await cliente.GetAsync("/Conta/Sair");

        Assert.NotEqual(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/Residentes")).StatusCode);
    }
}
