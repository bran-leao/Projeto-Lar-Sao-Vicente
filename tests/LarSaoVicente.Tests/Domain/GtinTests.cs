using LarSaoVicente.Domain.Common;

namespace LarSaoVicente.Tests.Domain;

/// <summary>
/// Testes da validação local de código de barras (US23, primeira camada).
/// </summary>
/// <remarks>
/// Os códigos usados aqui são de produtos de consumo comuns, escolhidos por serem
/// GTIN reais e verificáveis. Nenhum dado da instituição é utilizado.
/// </remarks>
public class GtinTests
{
    /// <summary>EAN-13 válido, com dígito verificador 7.</summary>
    private const string Ean13Valido = "7891000315507";

    /// <summary>EAN-8 válido, com dígito verificador 4.</summary>
    private const string Ean8Valido = "96385074";

    [Fact]
    public void IsValid_ComEan13Correto_Aceita()
    {
        Assert.True(Gtin.IsValid(Ean13Valido));
    }

    [Fact]
    public void IsValid_ComEan8Correto_Aceita()
    {
        Assert.True(Gtin.IsValid(Ean8Valido));
    }

    [Fact]
    public void IsValid_ComDigitoVerificadorErrado_Recusa()
    {
        // Mesmo código, apenas o último dígito trocado.
        Assert.False(Gtin.IsValid("7891000315508"));
    }

    [Fact]
    public void IsValid_ComDigitoTrocadoNoMeio_Recusa()
    {
        Assert.False(Gtin.IsValid("7891000415507"));
    }

    [Theory]
    [InlineData("789100031550")]      // 12 dígitos, mas não é um UPC-A válido
    [InlineData("78910003155071")]    // 14 dígitos com verificador incoerente
    [InlineData("789")]               // curto demais
    [InlineData("")]
    [InlineData(null)]
    public void IsValid_ComEntradaMalFormada_Recusa(string? codigo)
    {
        Assert.False(Gtin.IsValid(codigo));
    }

    [Fact]
    public void IsValid_ComLetras_Recusa()
    {
        Assert.False(Gtin.IsValid("789100031550X"));
    }

    [Fact]
    public void Normalize_RemoveEspacosEHifens()
    {
        // O leitor ou o digitador podem introduzir separadores.
        Assert.Equal(Ean13Valido, Gtin.Normalize("789 1000-3155 07"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Normalize_ComEntradaVazia_RetornaNulo(string? codigo)
    {
        Assert.Null(Gtin.Normalize(codigo));
    }

    [Fact]
    public void CalculateCheckDigit_ReproduzOVerificadorDoCodigoReal()
    {
        Assert.Equal(7, Gtin.CalculateCheckDigit("789100031550"));
        Assert.Equal(4, Gtin.CalculateCheckDigit("9638507"));
    }
}
