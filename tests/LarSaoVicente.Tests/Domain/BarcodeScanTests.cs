using LarSaoVicente.Domain.Common;

namespace LarSaoVicente.Tests.Domain;

/// <summary>
/// Testes da interpretação da leitura de código de barras e DataMatrix (US23).
/// </summary>
/// <remarks>
/// A instituição confirmou que as caixas recebidas trazem DataMatrix. Estes testes
/// montam conteúdos no formato do padrão GS1, com identificadores de aplicação, e
/// verificam que lote e validade são extraídos sem digitação.
/// </remarks>
public class BarcodeScanTests
{
    private const string Ean13 = "7891000315507";
    private const string Gtin14 = "07891000315507";

    /// <summary>Separador de campo variável do padrão GS1.</summary>
    private const char Gs = (char)29;

    [Fact]
    public void TryParse_ComCodigoDeBarrasTradicional_IdentificaOProduto()
    {
        Assert.True(BarcodeScan.TryParse(Ean13, out var leitura));

        Assert.Equal(ScannedCodeKind.LinearBarcode, leitura.Kind);
        Assert.Equal(Gtin14, leitura.Gtin);
        Assert.Null(leitura.Lot);
        Assert.Null(leitura.ExpiryDate);
        Assert.False(leitura.HasBatchData);
    }

    [Fact]
    public void TryParse_ComDataMatrixCompleto_ExtraiLoteValidadeESerie()
    {
        // 01 GTIN | 17 validade | 10 lote | 21 série
        var conteudo = $"01{Gtin14}17271031" + "10ABC1234" + Gs + "21SN0099887";

        Assert.True(BarcodeScan.TryParse(conteudo, out var leitura));

        Assert.Equal(ScannedCodeKind.Gs1DataMatrix, leitura.Kind);
        Assert.Equal(Gtin14, leitura.Gtin);
        Assert.Equal(new DateOnly(2027, 10, 31), leitura.ExpiryDate);
        Assert.Equal("ABC1234", leitura.Lot);
        Assert.Equal("SN0099887", leitura.SerialNumber);
        Assert.True(leitura.HasBatchData);
    }

    [Fact]
    public void TryParse_ComValidadeTerminadaEmZeroZero_UsaOUltimoDiaDoMes()
    {
        // O padrão GS1 admite dia "00" para "fim do mês", e é o caso mais comum em
        // medicamento, cuja caixa costuma trazer apenas mês e ano.
        var conteudo = $"01{Gtin14}17280200" + "10L1";

        Assert.True(BarcodeScan.TryParse(conteudo, out var leitura));

        Assert.Equal(new DateOnly(2028, 2, 29), leitura.ExpiryDate);
    }

    [Fact]
    public void TryParse_ComPrefixoDeSimbologiaDoLeitor_Ignora()
    {
        // Alguns leitores emitem "]d2" antes do conteúdo do DataMatrix.
        var conteudo = $"]d201{Gtin14}17300131" + "10LOTE9";

        Assert.True(BarcodeScan.TryParse(conteudo, out var leitura));

        Assert.Equal(Gtin14, leitura.Gtin);
        Assert.Equal("LOTE9", leitura.Lot);
    }

    [Fact]
    public void TryParse_ComLoteAntesDaValidade_LeNasDuasOrdens()
    {
        var conteudo = $"01{Gtin14}10LOTE7" + Gs + "17291130";

        Assert.True(BarcodeScan.TryParse(conteudo, out var leitura));

        Assert.Equal("LOTE7", leitura.Lot);
        Assert.Equal(new DateOnly(2029, 11, 30), leitura.ExpiryDate);
    }

    [Fact]
    public void TryParse_ComDataMatrixSemValidade_TrazApenasOQueExiste()
    {
        var conteudo = $"01{Gtin14}10LOTE3";

        Assert.True(BarcodeScan.TryParse(conteudo, out var leitura));

        Assert.Equal("LOTE3", leitura.Lot);
        Assert.Null(leitura.ExpiryDate);
        Assert.True(leitura.HasBatchData);
    }

    [Fact]
    public void TryParse_ComDataMatrixSoComGtin_NaoTrazDadosDeLote()
    {
        Assert.True(BarcodeScan.TryParse($"01{Gtin14}", out var leitura));

        Assert.Equal(ScannedCodeKind.Gs1DataMatrix, leitura.Kind);
        Assert.False(leitura.HasBatchData);
    }

    [Fact]
    public void TryParse_ComGtinDeDigitoVerificadorErrado_Recusa()
    {
        Assert.False(BarcodeScan.TryParse("0107891000315508" + "17271031", out _));
    }

    [Fact]
    public void TryParse_ComMesInvalidoNaValidade_IgnoraADataMasAceitaOProduto()
    {
        // Mês 13 não existe. Recusar a leitura inteira faria a caixa ficar fora do
        // controle por causa de um campo; melhor identificar o produto e pedir a data.
        Assert.True(BarcodeScan.TryParse($"01{Gtin14}17271331", out var leitura));

        Assert.Equal(Gtin14, leitura.Gtin);
        Assert.Null(leitura.ExpiryDate);
    }

    [Fact]
    public void TryParse_ComIdentificadorDesconhecido_MantemOQueJaFoiLido()
    {
        // Sem saber o comprimento do campo desconhecido não há como seguir com segurança.
        var conteudo = $"01{Gtin14}17271031" + "9912345";

        Assert.True(BarcodeScan.TryParse(conteudo, out var leitura));

        Assert.Equal(Gtin14, leitura.Gtin);
        Assert.Equal(new DateOnly(2027, 10, 31), leitura.ExpiryDate);
        Assert.Null(leitura.Lot);
    }

    [Fact]
    public void TryParse_ComDataMatrixSemGtin_Recusa()
    {
        Assert.False(BarcodeScan.TryParse("17271031" + "10LOTE1", out _));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("texto qualquer")]
    [InlineData("123")]
    public void TryParse_ComEntradaInvalida_Recusa(string? conteudo)
    {
        Assert.False(BarcodeScan.TryParse(conteudo, out _));
    }

    [Fact]
    public void TryParse_ComLoteAcimaDoLimiteDoPadrao_Recusa()
    {
        var loteLongo = new string('X', BarcodeScan.LotMaxLength + 1);

        Assert.False(BarcodeScan.TryParse($"01{Gtin14}10{loteLongo}", out _));
    }
}
