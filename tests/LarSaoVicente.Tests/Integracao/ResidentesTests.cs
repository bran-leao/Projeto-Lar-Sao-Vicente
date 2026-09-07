using System.Net;
using LarSaoVicente.Domain.Entities;
using LarSaoVicente.Domain.Enums;
using LarSaoVicente.Tests.Infraestrutura;
using Microsoft.EntityFrameworkCore;

namespace LarSaoVicente.Tests.Integracao;

/// <summary>
/// Testes de integração do cadastro de residentes (US02 a US06).
/// </summary>
/// <remarks>
/// Cada teste percorre o caminho completo — requisição HTTP, controller, domínio e
/// banco de dados — e confere o que efetivamente foi gravado, e não apenas o que a
/// tela apresenta.
/// </remarks>
public class ResidentesTests : IClassFixture<AplicacaoDeTeste>
{
    private readonly AplicacaoDeTeste _aplicacao;

    public ResidentesTests(AplicacaoDeTeste aplicacao)
    {
        _aplicacao = aplicacao;
    }

    private static Dictionary<string, string> DadosValidos(
        string nome,
        string nascimento = "1945-03-20",
        string entrada = "2020-06-01",
        string quarto = "12A",
        string grau = "2",
        string observacoes = "") => new()
    {
        ["FullName"] = nome,
        // O campo de data do navegador envia sempre no formato ISO (aaaa-MM-dd),
        // independentemente do idioma configurado.
        ["BirthDate"] = nascimento,
        ["AdmissionDate"] = entrada,
        ["Room"] = quarto,
        ["DependencyLevel"] = grau,
        ["Notes"] = observacoes
    };

    private async Task<HttpClient> ClienteAutenticadoAsync()
    {
        var cliente = _aplicacao.CriarCliente();
        await cliente.AutenticarAsync();
        return cliente;
    }

    /// <summary>
    /// Extrai apenas o corpo da tabela de resultados.
    /// </summary>
    /// <remarks>
    /// A verificação precisa recair sobre as linhas listadas, e não sobre a página
    /// inteira: a mensagem de sucesso do cadastro anterior continua em TempData e
    /// mencionaria o nome do residente fora da tabela.
    /// </remarks>
    private static string CorpoDaTabela(string html)
    {
        var inicio = html.IndexOf("<tbody>", StringComparison.Ordinal);
        var fim = html.IndexOf("</tbody>", StringComparison.Ordinal);

        return inicio >= 0 && fim > inicio ? html[inicio..fim] : string.Empty;
    }

    private Task<Resident?> BuscarPorNomeAsync(string nome) =>
        _aplicacao.ConsultarBancoAsync(contexto => contexto.Residents
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.FullName == nome));

    [Fact]
    public async Task Cadastrar_ComDadosValidos_PersisteNoBancoDeDados()
    {
        var cliente = await ClienteAutenticadoAsync();
        const string nome = "Cadastro Válido Teste";

        var resposta = await cliente.EnviarFormularioAsync(
            "/Residentes/Novo", "/Residentes/Novo",
            DadosValidos(nome, observacoes: "Observação de teste."));

        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);

        var residente = await BuscarPorNomeAsync(nome);
        Assert.NotNull(residente);
        Assert.Equal(new DateOnly(1945, 3, 20), residente.BirthDate);
        Assert.Equal(new DateOnly(2020, 6, 1), residente.AdmissionDate);
        Assert.Equal("12A", residente.Room);
        Assert.Equal(DependencyLevel.GrauII, residente.DependencyLevel);
        Assert.Equal(ResidentStatus.Ativo, residente.Status);
        Assert.Equal("Observação de teste.", residente.Notes);
    }

    /// <summary>
    /// Os campos de auditoria são preenchidos pelo contexto, e não pelo controller.
    /// Este teste confirma que a gravação registra quem realizou a operação.
    /// </summary>
    [Fact]
    public async Task Cadastrar_RegistraDataEResponsavelPelaOperacao()
    {
        var cliente = await ClienteAutenticadoAsync();
        const string nome = "Auditoria Teste";

        await cliente.EnviarFormularioAsync("/Residentes/Novo", "/Residentes/Novo", DadosValidos(nome));

        var residente = await BuscarPorNomeAsync(nome);
        Assert.NotNull(residente);
        Assert.NotEqual(default, residente.CreatedAt);
        Assert.False(string.IsNullOrWhiteSpace(residente.CreatedByUserId));
        Assert.Null(residente.UpdatedAt);
    }

    [Theory]
    [InlineData("2099-01-01", "2020-06-01", "nascimento")]
    [InlineData("1945-03-20", "2099-01-01", "entrada")]
    public async Task Cadastrar_ComDataNoFuturo_RejeitaENaoGrava(
        string nascimento, string entrada, string trechoEsperado)
    {
        var cliente = await ClienteAutenticadoAsync();
        var nome = $"Data Futura {trechoEsperado}";

        var resposta = await cliente.EnviarFormularioAsync(
            "/Residentes/Novo", "/Residentes/Novo",
            DadosValidos(nome, nascimento, entrada));

        // Permanece no formulário, exibindo o erro, em vez de redirecionar.
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var html = await resposta.Content.ReadAsStringAsync();
        Assert.Contains("no futuro", html);

        Assert.Null(await BuscarPorNomeAsync(nome));
    }

    [Fact]
    public async Task Cadastrar_SemOsCamposObrigatorios_ApresentaUmErroPorCampo()
    {
        var cliente = await ClienteAutenticadoAsync();

        var resposta = await cliente.EnviarFormularioAsync(
            "/Residentes/Novo", "/Residentes/Novo",
            DadosValidos(nome: "", nascimento: "", entrada: "", quarto: "", grau: ""));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var html = await resposta.Content.ReadAsStringAsync();
        Assert.Contains("Informe o nome completo.", html);
        Assert.Contains("Informe o quarto.", html);
        Assert.Contains("Informe a data de nascimento.", html);
        Assert.Contains("Selecione o grau de depend", html);
    }

    [Fact]
    public async Task Editar_AtualizaOsDadosEMantemOMesmoRegistro()
    {
        var cliente = await ClienteAutenticadoAsync();
        const string nome = "Edicao Teste";

        await cliente.EnviarFormularioAsync("/Residentes/Novo", "/Residentes/Novo", DadosValidos(nome));
        var original = await BuscarPorNomeAsync(nome);
        Assert.NotNull(original);

        var resposta = await cliente.EnviarFormularioAsync(
            $"/Residentes/{original.Id}/Editar",
            $"/Residentes/{original.Id}/Editar",
            DadosValidos(nome, quarto: "77B", grau: "3"));

        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);

        var atualizado = await BuscarPorNomeAsync(nome);
        Assert.NotNull(atualizado);
        Assert.Equal(original.Id, atualizado.Id);
        Assert.Equal("77B", atualizado.Room);
        Assert.Equal(DependencyLevel.GrauIII, atualizado.DependencyLevel);
        Assert.NotNull(atualizado.UpdatedAt);
        Assert.False(string.IsNullOrWhiteSpace(atualizado.UpdatedByUserId));
    }

    [Fact]
    public async Task Editar_ComDataNoFuturo_RejeitaEPreservaOsDadosGravados()
    {
        var cliente = await ClienteAutenticadoAsync();
        const string nome = "Edicao Invalida Teste";

        await cliente.EnviarFormularioAsync("/Residentes/Novo", "/Residentes/Novo", DadosValidos(nome));
        var original = await BuscarPorNomeAsync(nome);
        Assert.NotNull(original);

        var resposta = await cliente.EnviarFormularioAsync(
            $"/Residentes/{original.Id}/Editar",
            $"/Residentes/{original.Id}/Editar",
            DadosValidos(nome, nascimento: "2099-05-05", quarto: "99Z"));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var apos = await BuscarPorNomeAsync(nome);
        Assert.NotNull(apos);
        Assert.Equal(original.BirthDate, apos.BirthDate);
        Assert.Equal(original.Room, apos.Room);
    }

    [Fact]
    public async Task Inativar_AlteraASituacaoSemApagarOCadastro()
    {
        var cliente = await ClienteAutenticadoAsync();
        const string nome = "Inativacao Teste";

        await cliente.EnviarFormularioAsync("/Residentes/Novo", "/Residentes/Novo", DadosValidos(nome));
        var residente = await BuscarPorNomeAsync(nome);
        Assert.NotNull(residente);

        var resposta = await cliente.EnviarFormularioAsync(
            $"/Residentes/{residente.Id}",
            $"/Residentes/{residente.Id}/Inativar",
            []);

        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);

        var inativado = await BuscarPorNomeAsync(nome);
        Assert.NotNull(inativado);
        Assert.Equal(ResidentStatus.Inativo, inativado.Status);

        // O cadastro precisa continuar íntegro: a inativação não é uma exclusão.
        Assert.Equal(residente.Id, inativado.Id);
        Assert.Equal(residente.BirthDate, inativado.BirthDate);
        Assert.Equal(residente.AdmissionDate, inativado.AdmissionDate);
    }

    [Fact]
    public async Task Inativar_MantemOResidenteVisivelNaListagem()
    {
        var cliente = await ClienteAutenticadoAsync();
        const string nome = "Inativo Visivel Teste";

        await cliente.EnviarFormularioAsync("/Residentes/Novo", "/Residentes/Novo", DadosValidos(nome));
        var residente = await BuscarPorNomeAsync(nome);
        Assert.NotNull(residente);

        await cliente.EnviarFormularioAsync(
            $"/Residentes/{residente.Id}", $"/Residentes/{residente.Id}/Inativar", []);

        var linhas = CorpoDaTabela(await cliente.GetStringAsync("/Residentes"));
        Assert.Contains(nome, linhas);
        Assert.Contains("Inativo", linhas);
    }

    [Fact]
    public async Task Reativar_DevolveOResidenteParaASituacaoAtiva()
    {
        var cliente = await ClienteAutenticadoAsync();
        const string nome = "Reativacao Teste";

        await cliente.EnviarFormularioAsync("/Residentes/Novo", "/Residentes/Novo", DadosValidos(nome));
        var residente = await BuscarPorNomeAsync(nome);
        Assert.NotNull(residente);

        await cliente.EnviarFormularioAsync(
            $"/Residentes/{residente.Id}", $"/Residentes/{residente.Id}/Inativar", []);
        await cliente.EnviarFormularioAsync(
            $"/Residentes/{residente.Id}", $"/Residentes/{residente.Id}/Reativar", []);

        var reativado = await BuscarPorNomeAsync(nome);
        Assert.NotNull(reativado);
        Assert.Equal(ResidentStatus.Ativo, reativado.Status);
    }

    [Fact]
    public async Task Listagem_PesquisaPeloNomeEIgnoraOsDemais()
    {
        var cliente = await ClienteAutenticadoAsync();

        await cliente.EnviarFormularioAsync("/Residentes/Novo", "/Residentes/Novo",
            DadosValidos("Pesquisa Encontrada Teste"));
        await cliente.EnviarFormularioAsync("/Residentes/Novo", "/Residentes/Novo",
            DadosValidos("Pesquisa Ignorada Teste"));

        var linhas = CorpoDaTabela(await cliente.GetStringAsync("/Residentes?busca=Encontrada"));

        Assert.Contains("Pesquisa Encontrada Teste", linhas);
        Assert.DoesNotContain("Pesquisa Ignorada Teste", linhas);
    }

    [Fact]
    public async Task Listagem_QuandoAPesquisaNaoRetornaNada_ExibeMensagemApropriada()
    {
        var cliente = await ClienteAutenticadoAsync();

        var html = await cliente.GetStringAsync("/Residentes?busca=NomeQueNaoExisteNoSistema");

        Assert.Contains("Nenhum residente encontrado", html);
    }

    [Fact]
    public async Task Detalhes_DeResidenteInexistente_RetornaParaAListagemComAviso()
    {
        var cliente = await ClienteAutenticadoAsync();

        var resposta = await cliente.GetAsync($"/Residentes/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        Assert.Contains("/Residentes", resposta.Headers.Location?.OriginalString);
    }

    /// <summary>
    /// O identificador considerado é sempre o da rota. Sem isso, alterar o campo oculto
    /// do formulário permitiria gravar sobre o cadastro de outro residente.
    /// </summary>
    [Fact]
    public async Task Editar_IgnoraOIdentificadorEnviadoNoFormulario()
    {
        var cliente = await ClienteAutenticadoAsync();
        const string alvo = "Alvo Edicao Teste";
        const string vitima = "Vitima Edicao Teste";

        await cliente.EnviarFormularioAsync("/Residentes/Novo", "/Residentes/Novo", DadosValidos(alvo));
        await cliente.EnviarFormularioAsync("/Residentes/Novo", "/Residentes/Novo", DadosValidos(vitima));

        var residenteAlvo = await BuscarPorNomeAsync(alvo);
        var residenteVitima = await BuscarPorNomeAsync(vitima);
        Assert.NotNull(residenteAlvo);
        Assert.NotNull(residenteVitima);

        var campos = DadosValidos(alvo, quarto: "55C");
        campos["Id"] = residenteVitima.Id.ToString();

        await cliente.EnviarFormularioAsync(
            $"/Residentes/{residenteAlvo.Id}/Editar",
            $"/Residentes/{residenteAlvo.Id}/Editar",
            campos);

        var vitimaApos = await _aplicacao.ConsultarBancoAsync(c => c.Residents
            .AsNoTracking().FirstAsync(r => r.Id == residenteVitima.Id));

        Assert.Equal(vitima, vitimaApos.FullName);
        Assert.Equal("12A", vitimaApos.Room);
    }
}
