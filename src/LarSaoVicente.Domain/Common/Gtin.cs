namespace LarSaoVicente.Domain.Common;

/// <summary>
/// Validação local dos códigos de barras impressos nas embalagens.
/// </summary>
/// <remarks>
/// EAN-13, EAN-8, UPC-A e DUN-14 pertencem à mesma família (GTIN) e compartilham o
/// mesmo dígito verificador: cada dígito, da direita para a esquerda, é multiplicado
/// alternadamente por 3 e por 1, e o verificador é o que completa a soma até a próxima
/// dezena.
/// <para>
/// Refazer essa conta antes de consultar o banco é a primeira das três camadas de
/// validação descritas em <c>docs/arquitetura.md</c>, seção 5.1. Ela pega a leitura
/// truncada e o dígito digitado errado sem custo algum e, principalmente,
/// <b>sem depender de rede</b> — a leitura acontece no balcão, com alguém esperando.
/// </para>
/// </remarks>
public static class Gtin
{
    /// <summary>Comprimentos aceitos: EAN-8, UPC-A, EAN-13 e DUN-14.</summary>
    private static readonly int[] ComprimentosValidos = [8, 12, 13, 14];

    public const int MaxLength = 14;

    /// <summary>
    /// Remove espaços e hífens que o leitor ou o digitador possam ter introduzido.
    /// Retorna <c>null</c> para entrada vazia.
    /// </summary>
    public static string? Normalize(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var limpo = new string(code.Where(char.IsAsciiDigit).ToArray());
        return limpo.Length == 0 ? null : limpo;
    }

    /// <summary>
    /// Indica se o código é um GTIN bem formado, com dígito verificador coerente.
    /// </summary>
    /// <remarks>Espera o código já normalizado por <see cref="Normalize"/>.</remarks>
    public static bool IsValid(string? code)
    {
        if (string.IsNullOrEmpty(code) || !code.All(char.IsAsciiDigit))
        {
            return false;
        }

        if (!ComprimentosValidos.Contains(code.Length))
        {
            return false;
        }

        return CalculateCheckDigit(code[..^1]) == code[^1] - '0';
    }

    /// <summary>
    /// Traz o código para a forma canônica de 14 dígitos, completando com zeros à esquerda.
    /// </summary>
    /// <remarks>
    /// O EAN-13 impresso na caixa e o GTIN-14 que vem dentro do DataMatrix designam o
    /// <b>mesmo produto</b>: o segundo é o primeiro com um zero na frente. Guardar sempre
    /// em 14 dígitos é o que faz o índice único funcionar de verdade — sem isso, a mesma
    /// caixa cadastrada pela digitação e pela leitura viraria dois itens de catálogo.
    /// <para>
    /// O dígito verificador não muda: os pesos são atribuídos da direita para a esquerda,
    /// e o zero acrescentado à esquerda não soma nada.
    /// </para>
    /// </remarks>
    public static string ToGtin14(string code) => code.PadLeft(MaxLength, '0');

    /// <summary>
    /// Calcula o dígito verificador para o corpo do código, sem o último dígito.
    /// </summary>
    public static int CalculateCheckDigit(string payload)
    {
        var soma = 0;

        // Da direita para a esquerda: o dígito mais à direita do corpo pesa 3, o
        // seguinte pesa 1, e assim por diante.
        for (var i = 0; i < payload.Length; i++)
        {
            var digito = payload[payload.Length - 1 - i] - '0';
            soma += i % 2 == 0 ? digito * 3 : digito;
        }

        return (10 - soma % 10) % 10;
    }
}
