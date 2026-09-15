using System.Net;
using LarSaoVicente.Domain.Entities;
using LarSaoVicente.Domain.Enums;
using LarSaoVicente.Tests.Infraestrutura;
using Microsoft.EntityFrameworkCore;

namespace LarSaoVicente.Tests.Integracao;

/// <summary>
/// Testes de integração da entrada de medicamento e da conferência (US10, US26 e US27).
/// </summary>
public class EntradasTests : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _aplicacao = new();

    public Task InitializeAsync() => ((IAsyncLifetime)_aplicacao).InitializeAsync();

    public Task DisposeAsync() => ((IAsyncLifetime)_aplicacao).DisposeAsync();

    private async Task<HttpClient> ClienteAutenticadoAsync()
    {
        var cliente = _aplicacao.CriarCliente();
        await cliente.AutenticarAsync();
        return cliente;
    }

    /// <summary>Cria um item de catálogo direto no banco, para focar o teste na entrada.</summary>
    private async Task<Guid> CriarMedicamentoAsync(string nome = "Dipirona")
    {
        var item = new Medication(
            nome, "Dipirona sódica", "500 mg", PharmaceuticalForm.Sache,
            1, PackageUnit.Envelope, null, MedicationCategory.Medicamento, null);

        await _aplicacao.ExecutarNoBancoAsync(async contexto =>
        {
            contexto.Medications.Add(item);
            await contexto.SaveChangesAsync();
        });

        return item.Id;
    }

    private static Dictionary<string, string> DadosValidos(
        Guid medicamentoId,
        string quantidade = "11",
        string lote = "DIP7788",
        string validade = "2027-06-15",
        bool validadeNaoIdentificada = false,
        string origem = "5",
        string? recebimento = null,
        string serie = "",
        string observacoes = "")
    {
        var campos = new Dictionary<string, string>
        {
            ["MedicationId"] = medicamentoId.ToString(),
            ["Quantity"] = quantidade,
            ["LotNumber"] = lote,
            ["ExpiryDate"] = validadeNaoIdentificada ? string.Empty : validade,
            ["Origin"] = origem,
            ["ReceivedOn"] = recebimento ?? DateTime.UtcNow.ToString("yyyy-MM-dd"),
            ["SerialNumber"] = serie,
            ["Notes"] = observacoes,
            ["IdentifiedByScan"] = "false"
        };

        // Um checkbox não marcado simplesmente não é enviado pelo navegador.
        if (validadeNaoIdentificada)
        {
            campos["ExpiryNotIdentified"] = "true";
        }

        return campos;
    }

    private Task<List<MedicationEntry>> EntradasDoAsync(Guid medicamentoId) =>
        _aplicacao.ConsultarBancoAsync(c => c.MedicationEntries
            .AsNoTracking()
            .Where(e => e.MedicationId == medicamentoId)
            .ToListAsync());

    [Fact]
    public async Task Conferencia_SemAutenticacao_RedirecionaParaOLogin()
    {
        var cliente = _aplicacao.CriarCliente();

        var resposta = await cliente.GetAsync("/Entradas");

        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        Assert.Contains("/Conta/Login", resposta.Headers.Location?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task Registrar_OnzeEnvelopes_GravaUmRegistroSo()
    {
        // O pedido da enfermagem: não preencher o formulário onze vezes.
        var cliente = await ClienteAutenticadoAsync();
        var medicamentoId = await CriarMedicamentoAsync();

        var resposta = await cliente.EnviarFormularioAsync(
            $"/Entradas/Nova?medicamentoId={medicamentoId}", "/Entradas/Nova",
            DadosValidos(medicamentoId, quantidade: "11"));

        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);

        var entradas = await EntradasDoAsync(medicamentoId);

        Assert.Single(entradas);
        Assert.Equal(11, entradas[0].Quantity);
    }

    [Fact]
    public async Task Registrar_NasceAguardandoConferenciaENaoContaNoEstoque()
    {
        var cliente = await ClienteAutenticadoAsync();
        var medicamentoId = await CriarMedicamentoAsync();

        await cliente.EnviarFormularioAsync(
            $"/Entradas/Nova?medicamentoId={medicamentoId}", "/Entradas/Nova",
            DadosValidos(medicamentoId));

        var entradas = await EntradasDoAsync(medicamentoId);

        Assert.Equal(MedicationEntryStatus.AguardandoConferencia, entradas[0].Status);
        Assert.False(entradas[0].CountsTowardStock);

        // A tela do catálogo mostra zero em estoque enquanto ninguém conferir.
        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync($"/Medicamentos/{medicamentoId}"));
        Assert.Contains("Aguardando conferência", html);
    }

    [Fact]
    public async Task Registrar_SemValidadeENemAMarcacao_NaoGrava()
    {
        // Deixar em branco não pode ser o caminho fácil: validade desconhecida é o
        // principal risco em medicamento doado e precisa aparecer para quem confere.
        var cliente = await ClienteAutenticadoAsync();
        var medicamentoId = await CriarMedicamentoAsync();

        var campos = DadosValidos(medicamentoId);
        campos["ExpiryDate"] = string.Empty;

        var resposta = await cliente.EnviarFormularioAsync(
            $"/Entradas/Nova?medicamentoId={medicamentoId}", "/Entradas/Nova", campos);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Empty(await EntradasDoAsync(medicamentoId));
    }

    [Fact]
    public async Task Registrar_ComValidadeNaoIdentificada_Grava()
    {
        var cliente = await ClienteAutenticadoAsync();
        var medicamentoId = await CriarMedicamentoAsync();

        var resposta = await cliente.EnviarFormularioAsync(
            $"/Entradas/Nova?medicamentoId={medicamentoId}", "/Entradas/Nova",
            DadosValidos(medicamentoId, validadeNaoIdentificada: true));

        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);

        var entradas = await EntradasDoAsync(medicamentoId);

        Assert.True(entradas[0].ExpiryNotIdentified);
        Assert.Null(entradas[0].ExpiryDate);
    }

    [Fact]
    public async Task Registrar_ComQuantidadeZero_NaoGrava()
    {
        var cliente = await ClienteAutenticadoAsync();
        var medicamentoId = await CriarMedicamentoAsync();

        var resposta = await cliente.EnviarFormularioAsync(
            $"/Entradas/Nova?medicamentoId={medicamentoId}", "/Entradas/Nova",
            DadosValidos(medicamentoId, quantidade: "0"));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Empty(await EntradasDoAsync(medicamentoId));
    }

    [Fact]
    public async Task Conferir_LiberaParaOEstoqueERegistraOResponsavel()
    {
        var cliente = await ClienteAutenticadoAsync();
        var medicamentoId = await CriarMedicamentoAsync();

        await cliente.EnviarFormularioAsync(
            $"/Entradas/Nova?medicamentoId={medicamentoId}", "/Entradas/Nova",
            DadosValidos(medicamentoId));

        var entrada = (await EntradasDoAsync(medicamentoId))[0];

        var resposta = await cliente.EnviarFormularioAsync(
            "/Entradas", $"/Entradas/{entrada.Id}/Conferir", new Dictionary<string, string>());

        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);

        var depois = (await EntradasDoAsync(medicamentoId))[0];

        Assert.Equal(MedicationEntryStatus.Conferida, depois.Status);
        Assert.True(depois.CountsTowardStock);
        Assert.NotNull(depois.ReviewedAt);
        Assert.NotNull(depois.ReviewedByUserId);
    }

    [Fact]
    public async Task Conferir_FazOEstoqueAparecerNoCatalogo()
    {
        // O estoque é calculado, não transferido: passa a contar porque a situação mudou.
        var cliente = await ClienteAutenticadoAsync();
        var medicamentoId = await CriarMedicamentoAsync("Losartana");

        await cliente.EnviarFormularioAsync(
            $"/Entradas/Nova?medicamentoId={medicamentoId}", "/Entradas/Nova",
            DadosValidos(medicamentoId, quantidade: "7"));

        var entrada = (await EntradasDoAsync(medicamentoId))[0];

        await cliente.EnviarFormularioAsync(
            "/Entradas", $"/Entradas/{entrada.Id}/Conferir", new Dictionary<string, string>());

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync($"/Medicamentos/{medicamentoId}"));

        Assert.Contains("Conferida", html);
        Assert.Contains(">7<", html);
    }

    [Fact]
    public async Task Recusar_SemMotivo_NaoMudaASituacao()
    {
        var cliente = await ClienteAutenticadoAsync();
        var medicamentoId = await CriarMedicamentoAsync();

        await cliente.EnviarFormularioAsync(
            $"/Entradas/Nova?medicamentoId={medicamentoId}", "/Entradas/Nova",
            DadosValidos(medicamentoId));

        var entrada = (await EntradasDoAsync(medicamentoId))[0];

        await cliente.EnviarFormularioAsync(
            "/Entradas", $"/Entradas/{entrada.Id}/Recusar",
            new Dictionary<string, string> { ["motivo"] = "   " });

        var depois = (await EntradasDoAsync(medicamentoId))[0];

        Assert.Equal(MedicationEntryStatus.AguardandoConferencia, depois.Status);
    }

    [Fact]
    public async Task Recusar_ComMotivo_MantemORegistroForaDoEstoque()
    {
        // A entrada recusada não é apagada: é histórico e informação de gestão.
        var cliente = await ClienteAutenticadoAsync();
        var medicamentoId = await CriarMedicamentoAsync();

        await cliente.EnviarFormularioAsync(
            $"/Entradas/Nova?medicamentoId={medicamentoId}", "/Entradas/Nova",
            DadosValidos(medicamentoId));

        var entrada = (await EntradasDoAsync(medicamentoId))[0];

        await cliente.EnviarFormularioAsync(
            "/Entradas", $"/Entradas/{entrada.Id}/Recusar",
            new Dictionary<string, string> { ["motivo"] = "Lote vencido na chegada." });

        var depois = (await EntradasDoAsync(medicamentoId))[0];

        Assert.Equal(MedicationEntryStatus.Recusada, depois.Status);
        Assert.False(depois.CountsTowardStock);
        Assert.Equal("Lote vencido na chegada.", depois.RejectionReason);
    }

    [Fact]
    public async Task Editar_DepoisDeConferida_NaoAlteraAQuantidade()
    {
        var cliente = await ClienteAutenticadoAsync();
        var medicamentoId = await CriarMedicamentoAsync();

        await cliente.EnviarFormularioAsync(
            $"/Entradas/Nova?medicamentoId={medicamentoId}", "/Entradas/Nova",
            DadosValidos(medicamentoId, quantidade: "11"));

        var entrada = (await EntradasDoAsync(medicamentoId))[0];

        await cliente.EnviarFormularioAsync(
            "/Entradas", $"/Entradas/{entrada.Id}/Conferir", new Dictionary<string, string>());

        // Já conta no estoque: mudá-la em silêncio faria o sistema divergir da prateleira.
        var resposta = await cliente.EnviarFormularioAsync(
            "/Entradas", $"/Entradas/{entrada.Id}/Editar",
            DadosValidos(medicamentoId, quantidade: "999"));

        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);

        var depois = (await EntradasDoAsync(medicamentoId))[0];

        Assert.Equal(11, depois.Quantity);
    }

    [Fact]
    public async Task Editar_ComIdDaRotaDiferenteDoFormulario_AlteraAEntradaDaRota()
    {
        // Regressão do defeito 5 da Sprint 1, agora na tela de entradas.
        var cliente = await ClienteAutenticadoAsync();
        var medicamentoId = await CriarMedicamentoAsync();

        await cliente.EnviarFormularioAsync(
            $"/Entradas/Nova?medicamentoId={medicamentoId}", "/Entradas/Nova",
            DadosValidos(medicamentoId, quantidade: "3", lote: "ALVO"));
        await cliente.EnviarFormularioAsync(
            $"/Entradas/Nova?medicamentoId={medicamentoId}", "/Entradas/Nova",
            DadosValidos(medicamentoId, quantidade: "5", lote: "VITIMA"));

        var entradas = await EntradasDoAsync(medicamentoId);
        var alvo = entradas.Single(e => e.LotNumber == "ALVO");
        var vitima = entradas.Single(e => e.LotNumber == "VITIMA");

        var campos = DadosValidos(medicamentoId, quantidade: "42", lote: "ALTERADO");
        campos["Id"] = vitima.Id.ToString();

        await cliente.EnviarFormularioAsync(
            "/Entradas", $"/Entradas/{alvo.Id}/Editar", campos);

        var depois = await EntradasDoAsync(medicamentoId);

        Assert.Equal(42, depois.Single(e => e.Id == alvo.Id).Quantity);
        Assert.Equal(5, depois.Single(e => e.Id == vitima.Id).Quantity);
        Assert.Equal("VITIMA", depois.Single(e => e.Id == vitima.Id).LotNumber);
    }

    [Fact]
    public async Task Conferencia_SemPendencias_ExibeEstadoVazio()
    {
        var cliente = await ClienteAutenticadoAsync();

        var html = WebUtility.HtmlDecode(await cliente.GetStringAsync("/Entradas"));

        Assert.Contains("Nada pendente", html);
    }
}
