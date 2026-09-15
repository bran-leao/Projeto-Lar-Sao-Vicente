using System.Net;
using LarSaoVicente.Domain.Entities;
using LarSaoVicente.Domain.Enums;
using LarSaoVicente.Tests.Infraestrutura;
using Microsoft.EntityFrameworkCore;

namespace LarSaoVicente.Tests.Integracao;

/// <summary>
/// Testes de integração do catálogo e da leitura de código (US09, US23 e US24).
/// </summary>
/// <remarks>
/// Cada teste percorre o caminho completo — requisição HTTP, controller, domínio e
/// banco — e confere o que foi gravado, não apenas o que a tela mostra.
/// </remarks>
public class MedicamentosTests : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _aplicacao = new();

    /// <summary>Separador de campo variável do padrão GS1, como o leitor o emite.</summary>
    private const char Gs = (char)29;

    private const string Ean13 = "7891000315507";
    private const string Gtin14 = "07891000315507";

    public Task InitializeAsync() => ((IAsyncLifetime)_aplicacao).InitializeAsync();

    public Task DisposeAsync() => ((IAsyncLifetime)_aplicacao).DisposeAsync();

    private async Task<HttpClient> ClienteAutenticadoAsync()
    {
        var cliente = _aplicacao.CriarCliente();
        await cliente.AutenticarAsync();
        return cliente;
    }

    private static Dictionary<string, string> DadosValidos(
        string nome = "Macrodantina",
        string principioAtivo = "Nitrofurantoína",
        string concentracao = "100 mg",
        string forma = "1",
        string porEmbalagem = "28",
        string unidade = "1",
        string codigo = "",
        string categoria = "1",
        string observacoes = "") => new()
    {
        ["CommercialName"] = nome,
        ["ActiveIngredient"] = principioAtivo,
        ["Strength"] = concentracao,
        ["Form"] = forma,
        ["UnitsPerPackage"] = porEmbalagem,
        ["PackageUnit"] = unidade,
        ["Barcode"] = codigo,
        ["Category"] = categoria,
        ["Notes"] = observacoes,
        ["VeioDeLeitura"] = "false"
    };

    /// <summary>
    /// Lê o corpo da resposta já decodificado.
    /// </summary>
    /// <remarks>
    /// O Razor codifica os acentos como entidades HTML — "não" vira "n&amp;#xE3;o" no
    /// código-fonte da página. Decodificar aqui faz o teste comparar com o texto que o
    /// funcionário realmente lê na tela.
    /// </remarks>
    private static async Task<string> LerTextoAsync(HttpResponseMessage resposta)
        => WebUtility.HtmlDecode(await resposta.Content.ReadAsStringAsync());

    private Task<Medication?> BuscarPorNomeAsync(string nome) =>
        _aplicacao.ConsultarBancoAsync(c => c.Medications
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.CommercialName == nome));

    [Fact]
    public async Task Catalogo_SemAutenticacao_RedirecionaParaOLogin()
    {
        var cliente = _aplicacao.CriarCliente();

        var resposta = await cliente.GetAsync("/Medicamentos");

        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        Assert.Contains("/Conta/Login", resposta.Headers.Location?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task Cadastrar_ComDadosValidos_GravaNoBanco()
    {
        var cliente = await ClienteAutenticadoAsync();

        var resposta = await cliente.EnviarFormularioAsync(
            "/Medicamentos/Novo", "/Medicamentos/Novo", DadosValidos());

        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);

        var gravado = await BuscarPorNomeAsync("Macrodantina");

        Assert.NotNull(gravado);
        Assert.Equal("Nitrofurantoína", gravado.ActiveIngredient);
        Assert.Equal(28, gravado.UnitsPerPackage);
        Assert.Equal(MedicationStatus.Ativo, gravado.Status);
    }

    [Fact]
    public async Task Cadastrar_MedicamentoSemPrincipioAtivo_NaoGrava()
    {
        var cliente = await ClienteAutenticadoAsync();

        var resposta = await cliente.EnviarFormularioAsync(
            "/Medicamentos/Novo", "/Medicamentos/Novo",
            DadosValidos(nome: "Sem princípio", principioAtivo: ""));

        // Volta o formulário, e não um redirecionamento.
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Null(await BuscarPorNomeAsync("Sem princípio"));
    }

    [Fact]
    public async Task Cadastrar_InsumoSemPrincipioAtivo_Grava()
    {
        var cliente = await ClienteAutenticadoAsync();

        var resposta = await cliente.EnviarFormularioAsync(
            "/Medicamentos/Novo", "/Medicamentos/Novo",
            DadosValidos(nome: "Luva de procedimento", principioAtivo: "", forma: "",
                porEmbalagem: "100", categoria: "2"));

        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);

        var gravado = await BuscarPorNomeAsync("Luva de procedimento");

        Assert.NotNull(gravado);
        Assert.Equal(MedicationCategory.Insumo, gravado.Category);
        Assert.Null(gravado.ActiveIngredient);
    }

    [Fact]
    public async Task Cadastrar_ComCodigoDeBarrasInvalido_NaoGrava()
    {
        var cliente = await ClienteAutenticadoAsync();

        var resposta = await cliente.EnviarFormularioAsync(
            "/Medicamentos/Novo", "/Medicamentos/Novo",
            DadosValidos(nome: "Código torto", codigo: "7891000315508"));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Null(await BuscarPorNomeAsync("Código torto"));
    }

    [Fact]
    public async Task Cadastrar_ComCodigoJaUsado_AvisaEmVezDeQuebrar()
    {
        var cliente = await ClienteAutenticadoAsync();

        await cliente.EnviarFormularioAsync("/Medicamentos/Novo", "/Medicamentos/Novo",
            DadosValidos(nome: "Primeiro item", codigo: Ean13));

        var resposta = await cliente.EnviarFormularioAsync("/Medicamentos/Novo", "/Medicamentos/Novo",
            DadosValidos(nome: "Segundo item", codigo: Ean13));

        // Melhor avisar no formulário do que deixar o banco recusar com uma mensagem
        // que o funcionário não teria como interpretar.
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Contains("já está cadastrado", await LerTextoAsync(resposta));
        Assert.Null(await BuscarPorNomeAsync("Segundo item"));
    }

    [Fact]
    public async Task Cadastrar_PeloEanDigitado_GuardaEmQuatorzeDigitos()
    {
        var cliente = await ClienteAutenticadoAsync();

        await cliente.EnviarFormularioAsync("/Medicamentos/Novo", "/Medicamentos/Novo",
            DadosValidos(nome: "Canônico", codigo: Ean13));

        var gravado = await BuscarPorNomeAsync("Canônico");

        Assert.Equal(Gtin14, gravado!.Barcode);
    }

    [Fact]
    public async Task Buscar_PeloPrincipioAtivo_EncontraPeloNomeComercial()
    {
        // É o achado das planilhas: o mesmo item aparece ora como Macrodantina, ora
        // como Nitrofurantoína, e nada relaciona os dois registros.
        var cliente = await ClienteAutenticadoAsync();

        await cliente.EnviarFormularioAsync("/Medicamentos/Novo", "/Medicamentos/Novo", DadosValidos());

        var html = WebUtility.HtmlDecode(
            await cliente.GetStringAsync("/Medicamentos?busca=Nitrofurantoína"));

        Assert.Contains("Macrodantina", html);
    }

    [Fact]
    public async Task Ler_CodigoDesconhecido_AbreOCadastroComOCodigoPreenchido()
    {
        var cliente = await ClienteAutenticadoAsync();

        var resposta = await cliente.EnviarFormularioAsync(
            "/Medicamentos/Identificar", "/Medicamentos/Identificar",
            new Dictionary<string, string> { ["Codigo"] = Ean13 });

        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);

        var destino = resposta.Headers.Location!.ToString();
        Assert.Contains("/Medicamentos/Novo", destino);
        Assert.Contains(Gtin14, destino);
    }

    [Fact]
    public async Task Ler_DataMatrixDeItemConhecido_LevaParaAEntradaComLoteEValidade()
    {
        var cliente = await ClienteAutenticadoAsync();

        await cliente.EnviarFormularioAsync("/Medicamentos/Novo", "/Medicamentos/Novo",
            DadosValidos(nome: "Losartana", principioAtivo: "Losartana potássica",
                concentracao: "50 mg", porEmbalagem: "30", codigo: Ean13));

        var conteudo = $"01{Gtin14}17281130" + "10LOTE-XYZ" + Gs + "21SN777";

        var resposta = await cliente.EnviarFormularioAsync(
            "/Medicamentos/Identificar", "/Medicamentos/Identificar",
            new Dictionary<string, string> { ["Codigo"] = conteudo });

        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);

        var destino = resposta.Headers.Location!.ToString();
        Assert.Contains("/Entradas/Nova", destino);
        Assert.Contains("lote=LOTE-XYZ", destino);
        Assert.Contains("2028-11-30", destino);
        Assert.Contains("serie=SN777", destino);
    }

    [Fact]
    public async Task Ler_CodigoComDigitoVerificadorErrado_NaoRedireciona()
    {
        var cliente = await ClienteAutenticadoAsync();

        var resposta = await cliente.EnviarFormularioAsync(
            "/Medicamentos/Identificar", "/Medicamentos/Identificar",
            new Dictionary<string, string> { ["Codigo"] = "7891000315508" });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Contains("não reconhecido", await LerTextoAsync(resposta));
    }

    [Fact]
    public async Task Editar_ComIdDaRotaDiferenteDoFormulario_GravaNoItemDaRota()
    {
        // Regressão do defeito 5 da Sprint 1: sem [FromRoute], o provedor de formulário
        // tem precedência sobre o de rota e um campo oculto alterado no navegador
        // gravaria sobre o registro de outro item.
        var cliente = await ClienteAutenticadoAsync();

        await cliente.EnviarFormularioAsync("/Medicamentos/Novo", "/Medicamentos/Novo",
            DadosValidos(nome: "Alvo legítimo"));
        await cliente.EnviarFormularioAsync("/Medicamentos/Novo", "/Medicamentos/Novo",
            DadosValidos(nome: "Vítima"));

        var alvo = await BuscarPorNomeAsync("Alvo legítimo");
        var vitima = await BuscarPorNomeAsync("Vítima");

        var campos = DadosValidos(nome: "Nome alterado");
        campos["Id"] = vitima!.Id.ToString();

        await cliente.EnviarFormularioAsync(
            $"/Medicamentos/{alvo!.Id}/Editar", $"/Medicamentos/{alvo.Id}/Editar", campos);

        var alvoDepois = await _aplicacao.ConsultarBancoAsync(
            c => c.Medications.AsNoTracking().SingleAsync(m => m.Id == alvo.Id));
        var vitimaDepois = await _aplicacao.ConsultarBancoAsync(
            c => c.Medications.AsNoTracking().SingleAsync(m => m.Id == vitima.Id));

        Assert.Equal("Nome alterado", alvoDepois.CommercialName);
        Assert.Equal("Vítima", vitimaDepois.CommercialName);
    }

    [Fact]
    public async Task Inativar_MantemOItemEOHistorico()
    {
        var cliente = await ClienteAutenticadoAsync();

        await cliente.EnviarFormularioAsync("/Medicamentos/Novo", "/Medicamentos/Novo",
            DadosValidos(nome: "Para inativar"));

        var item = await BuscarPorNomeAsync("Para inativar");

        var resposta = await cliente.EnviarFormularioAsync(
            $"/Medicamentos/{item!.Id}", $"/Medicamentos/{item.Id}/Inativar",
            new Dictionary<string, string>());

        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);

        var depois = await BuscarPorNomeAsync("Para inativar");

        Assert.NotNull(depois);
        Assert.Equal(MedicationStatus.Inativo, depois.Status);
    }

    [Fact]
    public async Task Catalogo_Vazio_ExibeEstadoVazio()
    {
        var cliente = await ClienteAutenticadoAsync();

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/Medicamentos"));

        Assert.Contains("catálogo está vazio", html);
    }
}
