using LarSaoVicente.Domain.Common;
using LarSaoVicente.Domain.Entities;
using LarSaoVicente.Domain.Enums;

namespace LarSaoVicente.Tests.Domain;

/// <summary>
/// Testes das regras da entrada de medicamento (US10 e US26).
/// </summary>
public class MedicationEntryTests
{
    private static readonly DateOnly Hoje = new(2026, 9, 15);
    private static readonly DateTime AgoraUtc = new(2026, 9, 15, 13, 40, 0, DateTimeKind.Utc);
    private static readonly Guid Medicamento = Guid.NewGuid();

    private static MedicationEntry CriarEntradaValida(
        int quantidade = 11,
        string? lote = "L2026A",
        DateOnly? validade = null,
        bool validadeNaoIdentificada = false,
        MedicationOrigin origem = MedicationOrigin.Doacao,
        DateOnly? recebimento = null,
        bool porLeitura = false,
        string? serie = null,
        string? observacoes = null)
    {
        return new MedicationEntry(
            Medicamento,
            quantidade,
            lote,
            validadeNaoIdentificada ? null : validade ?? new DateOnly(2027, 10, 31),
            validadeNaoIdentificada,
            origem,
            recebimento ?? Hoje,
            porLeitura,
            serie,
            observacoes,
            Hoje);
    }

    [Fact]
    public void Criar_ComDadosValidos_NasceAguardandoConferencia()
    {
        var entrada = CriarEntradaValida();

        Assert.NotEqual(Guid.Empty, entrada.Id);
        Assert.Equal(Medicamento, entrada.MedicationId);
        Assert.Equal(11, entrada.Quantity);
        Assert.Equal("L2026A", entrada.LotNumber);
        Assert.Equal(new DateOnly(2027, 10, 31), entrada.ExpiryDate);
        Assert.Equal(MedicationOrigin.Doacao, entrada.Origin);
        Assert.Equal(MedicationEntryStatus.AguardandoConferencia, entrada.Status);
        Assert.False(entrada.CountsTowardStock);
    }

    [Fact]
    public void Criar_OnzeEnvelopes_EUmRegistroSo()
    {
        // O pedido da enfermagem: não preencher o formulário onze vezes.
        var entrada = CriarEntradaValida(quantidade: 11);

        Assert.Equal(11, entrada.Quantity);
    }

    [Fact]
    public void Criar_SemMedicamento_Rejeita()
    {
        var excecao = Assert.Throws<DomainValidationException>(() => new MedicationEntry(
            Guid.Empty, 1, null, new DateOnly(2027, 1, 31), false,
            MedicationOrigin.Doacao, Hoje, false, null, null, Hoje));

        Assert.Contains("medicamento", excecao.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Criar_ComQuantidadeNaoPositiva_Rejeita(int quantidade)
    {
        var excecao = Assert.Throws<DomainValidationException>(() => CriarEntradaValida(quantidade: quantidade));

        Assert.Contains("quantidade", excecao.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Criar_ComQuantidadeAcimaDoLimite_Rejeita()
    {
        Assert.Throws<DomainValidationException>(
            () => CriarEntradaValida(quantidade: MedicationEntry.MaxQuantity + 1));
    }

    [Fact]
    public void Criar_ComValidadeNaoIdentificada_Aceita()
    {
        // Caso mais comum em cartela avulsa de doação.
        var entrada = CriarEntradaValida(validadeNaoIdentificada: true);

        Assert.True(entrada.ExpiryNotIdentified);
        Assert.Null(entrada.ExpiryDate);
    }

    [Fact]
    public void Criar_SemValidadeENemAMarcacao_Rejeita()
    {
        // Deixar em branco não pode ser o caminho fácil: validade desconhecida é o
        // principal risco em medicamento doado e precisa aparecer para quem confere.
        var excecao = Assert.Throws<DomainValidationException>(() => new MedicationEntry(
            Medicamento, 1, null, null, false,
            MedicationOrigin.Doacao, Hoje, false, null, null, Hoje));

        Assert.Contains("validade", excecao.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Criar_ComValidadeEMarcacaoAoMesmoTempo_Rejeita()
    {
        var excecao = Assert.Throws<DomainValidationException>(() => new MedicationEntry(
            Medicamento, 1, null, new DateOnly(2027, 1, 31), true,
            MedicationOrigin.Doacao, Hoje, false, null, null, Hoje));

        Assert.Contains("validade", excecao.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Criar_ComValidadeJaVencida_Aceita()
    {
        // Doação vencida precisa ser registrada para poder ser recusada e virar
        // informação de gestão. Recusar o cadastro apagaria o fato.
        var entrada = CriarEntradaValida(validade: new DateOnly(2026, 1, 31));

        Assert.True(entrada.IsExpiredOn(Hoje));
    }

    [Fact]
    public void Criar_ComRecebimentoNoFuturo_Rejeita()
    {
        var excecao = Assert.Throws<DomainValidationException>(
            () => CriarEntradaValida(recebimento: Hoje.AddDays(1)));

        Assert.Contains("recebimento", excecao.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Criar_ComOrigemInvalida_Rejeita()
    {
        Assert.Throws<DomainValidationException>(() => CriarEntradaValida(origem: (MedicationOrigin)77));
    }

    [Fact]
    public void Criar_ComLoteAcimaDoLimite_Rejeita()
    {
        Assert.Throws<DomainValidationException>(
            () => CriarEntradaValida(lote: new string('L', MedicationEntry.LotNumberMaxLength + 1)));
    }

    [Fact]
    public void Criar_PorLeitura_RegistraAProcedencia()
    {
        var entrada = CriarEntradaValida(porLeitura: true, serie: "SN12345");

        Assert.True(entrada.IdentifiedByScan);
        Assert.Equal("SN12345", entrada.SerialNumber);

        // Mesmo lida, a entrada nasce pendente: quem confere decide.
        Assert.Equal(MedicationEntryStatus.AguardandoConferencia, entrada.Status);
    }

    [Fact]
    public void AlterarQuantidade_ComEntradaPendente_Ajusta()
    {
        var entrada = CriarEntradaValida(quantidade: 11);

        entrada.ChangeQuantity(9);

        Assert.Equal(9, entrada.Quantity);
    }

    [Fact]
    public void AlterarQuantidade_ComValorInvalido_Rejeita()
    {
        var entrada = CriarEntradaValida();

        Assert.Throws<DomainValidationException>(() => entrada.ChangeQuantity(0));
        Assert.Equal(11, entrada.Quantity);
    }

    [Fact]
    public void Conferir_LiberaParaOEstoqueERegistraOResponsavel()
    {
        var entrada = CriarEntradaValida();

        entrada.Confirm("usuario-1", AgoraUtc);

        Assert.Equal(MedicationEntryStatus.Conferida, entrada.Status);
        Assert.True(entrada.CountsTowardStock);
        Assert.Equal("usuario-1", entrada.ReviewedByUserId);
        Assert.Equal(AgoraUtc, entrada.ReviewedAt);
    }

    [Fact]
    public void Recusar_ExigeMotivoEMantemForaDoEstoque()
    {
        var entrada = CriarEntradaValida();

        entrada.Reject("Lote vencido na chegada.", "usuario-1", AgoraUtc);

        Assert.Equal(MedicationEntryStatus.Recusada, entrada.Status);
        Assert.False(entrada.CountsTowardStock);
        Assert.Equal("Lote vencido na chegada.", entrada.RejectionReason);
    }

    [Fact]
    public void Recusar_SemMotivo_Rejeita()
    {
        var entrada = CriarEntradaValida();

        var excecao = Assert.Throws<DomainValidationException>(() => entrada.Reject("  ", "usuario-1", AgoraUtc));

        Assert.Contains("motivo", excecao.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(MedicationEntryStatus.AguardandoConferencia, entrada.Status);
    }

    [Fact]
    public void Conferir_DuasVezes_Rejeita()
    {
        var entrada = CriarEntradaValida();
        entrada.Confirm("usuario-1", AgoraUtc);

        Assert.Throws<DomainValidationException>(() => entrada.Confirm("usuario-2", AgoraUtc));
    }

    [Fact]
    public void Alterar_DepoisDeConferida_Rejeita()
    {
        // Depois de conferida o item já conta no estoque; mudá-lo em silêncio faria o
        // número do sistema divergir da prateleira sem deixar rastro.
        var entrada = CriarEntradaValida();
        entrada.Confirm("usuario-1", AgoraUtc);

        Assert.Throws<DomainValidationException>(() => entrada.ChangeQuantity(5));
        Assert.Throws<DomainValidationException>(() => entrada.Update(
            5, "L9", new DateOnly(2028, 1, 31), false, MedicationOrigin.Familia, Hoje, null, null, Hoje));
    }

    [Fact]
    public void Atualizar_ComEntradaPendente_AplicaAsMesmasValidacoes()
    {
        var entrada = CriarEntradaValida();

        entrada.Update(
            20, "L2026B", null, true, MedicationOrigin.Familia, Hoje.AddDays(-2), "SN1", "Trazido pela família.", Hoje);

        Assert.Equal(20, entrada.Quantity);
        Assert.True(entrada.ExpiryNotIdentified);
        Assert.Equal(MedicationOrigin.Familia, entrada.Origin);

        Assert.Throws<DomainValidationException>(() => entrada.Update(
            0, null, new DateOnly(2027, 1, 31), false, MedicationOrigin.Doacao, Hoje, null, null, Hoje));
    }

    [Fact]
    public void IsExpiredOn_ComValidadeNaoIdentificada_NaoEhTratadaComoVencida()
    {
        var entrada = CriarEntradaValida(validadeNaoIdentificada: true);

        Assert.False(entrada.IsExpiredOn(Hoje));
    }
}
