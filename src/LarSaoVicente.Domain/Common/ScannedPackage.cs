namespace LarSaoVicente.Domain.Common;

/// <summary>
/// O que uma leitura de código conseguiu extrair da embalagem.
/// </summary>
/// <param name="Gtin">Identificador do produto, em 14 dígitos. Sempre presente em leitura válida.</param>
/// <param name="Lot">Lote, quando o código o carregar.</param>
/// <param name="ExpiryDate">Validade, quando o código a carregar.</param>
/// <param name="SerialNumber">Número de série da caixa específica, quando presente.</param>
/// <param name="Kind">Que tipo de código foi lido.</param>
/// <remarks>
/// É o resultado da segunda camada de validação descrita em <c>docs/arquitetura.md</c>,
/// seção 5.1: o comprimento e a estrutura dizem se veio um código de barras tradicional
/// ou um DataMatrix, e o que cada um é capaz de informar.
/// </remarks>
public readonly record struct ScannedPackage(
    string Gtin,
    string? Lot,
    DateOnly? ExpiryDate,
    string? SerialNumber,
    ScannedCodeKind Kind)
{
    /// <summary>
    /// Indica se a leitura trouxe lote e validade, dispensando a digitação.
    /// </summary>
    public bool HasBatchData => Lot is not null || ExpiryDate is not null;
}

/// <summary>Tipo de código lido.</summary>
public enum ScannedCodeKind
{
    /// <summary>Código de barras tradicional. Identifica o produto, nunca a caixa específica.</summary>
    LinearBarcode = 1,

    /// <summary>DataMatrix no padrão GS1. Identifica a caixa, com lote, validade e série.</summary>
    Gs1DataMatrix = 2
}
