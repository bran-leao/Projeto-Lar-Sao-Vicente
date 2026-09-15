namespace LarSaoVicente.Domain.Common;

/// <summary>
/// Interpreta o que o leitor digitou no campo, seja código de barras ou DataMatrix.
/// </summary>
/// <remarks>
/// O leitor USB opera em modo HID: para a aplicação ele é um teclado que digita o
/// conteúdo do código e pressiona Enter. Cabe a esta classe descobrir o que foi digitado.
/// <para>
/// A instituição confirmou que as caixas recebidas trazem DataMatrix, o que permite
/// capturar lote e validade na leitura em vez de exigir digitação a cada entrada. O
/// sistema continua aceitando o código de barras tradicional, porque cartela avulsa e
/// caixa antiga podem não ter o código bidimensional.
/// </para>
/// <para>
/// Tudo acontece localmente, sem consulta a serviço externo. Ver
/// <c>docs/arquitetura.md</c>, seção 5.1.
/// </para>
/// </remarks>
public static class BarcodeScan
{
    /// <summary>Separador de campo de comprimento variável do padrão GS1 (ASCII 29).</summary>
    private const char GroupSeparator = (char)29;

    /// <summary>
    /// Identificadores de aplicação de comprimento fixo que interessam ao projeto.
    /// Os demais são tratados como variáveis.
    /// </summary>
    private static readonly Dictionary<string, int> ComprimentosFixos = new()
    {
        ["00"] = 18,  // SSCC, unidade logística
        ["01"] = 14,  // GTIN do produto
        ["11"] = 6,   // data de fabricação
        ["15"] = 6,   // durabilidade mínima
        ["17"] = 6    // validade
    };

    private const string AiGtin = "01";
    private const string AiValidade = "17";
    private const string AiLote = "10";
    private const string AiSerie = "21";

    /// <summary>Tamanho máximo de lote e série no padrão GS1.</summary>
    public const int LotMaxLength = 20;

    /// <summary>
    /// Tenta interpretar a leitura. Retorna <c>false</c> quando o conteúdo não é um
    /// código reconhecível ou o dígito verificador não confere.
    /// </summary>
    public static bool TryParse(string? raw, out ScannedPackage package)
    {
        package = default;

        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var conteudo = RemoveSymbologyIdentifier(raw.Trim());

        // Só dígitos e comprimento de GTIN: é o código de barras tradicional, que
        // identifica o produto e não diz nada sobre a caixa.
        var linear = Gtin.Normalize(conteudo);
        if (linear is not null && linear.Length == conteudo.Length && Gtin.IsValid(linear))
        {
            package = new ScannedPackage(
                Gtin.ToGtin14(linear), Lot: null, ExpiryDate: null, SerialNumber: null,
                ScannedCodeKind.LinearBarcode);
            return true;
        }

        return TryParseGs1(conteudo, out package);
    }

    /// <summary>
    /// Percorre os pares "identificador de aplicação + valor" de um DataMatrix GS1.
    /// </summary>
    private static bool TryParseGs1(string conteudo, out ScannedPackage package)
    {
        package = default;

        string? gtin = null, lote = null, serie = null;
        DateOnly? validade = null;
        var posicao = 0;

        while (posicao + 2 <= conteudo.Length)
        {
            var ai = conteudo.Substring(posicao, 2);
            if (!ai.All(char.IsAsciiDigit))
            {
                break;
            }

            posicao += 2;
            string valor;

            if (ComprimentosFixos.TryGetValue(ai, out var comprimento))
            {
                if (posicao + comprimento > conteudo.Length)
                {
                    break;
                }

                valor = conteudo.Substring(posicao, comprimento);
                posicao += comprimento;
            }
            else if (ai is AiLote or AiSerie)
            {
                // Campo de comprimento variável: vai até o separador ou até o fim.
                var fim = conteudo.IndexOf(GroupSeparator, posicao);
                if (fim < 0)
                {
                    fim = conteudo.Length;
                }

                valor = conteudo[posicao..fim];
                posicao = fim < conteudo.Length ? fim + 1 : fim;
            }
            else
            {
                // Identificador que o projeto não conhece: sem saber seu comprimento não
                // há como continuar com segurança. Interrompe e devolve o que já foi lido,
                // em vez de arriscar interpretar lixo como lote ou validade.
                break;
            }

            switch (ai)
            {
                case AiGtin: gtin = valor; break;
                case AiValidade: validade = ParseValidade(valor); break;
                case AiLote: lote = Vazio(valor); break;
                case AiSerie: serie = Vazio(valor); break;
            }
        }

        if (gtin is null || !Gtin.IsValid(gtin))
        {
            return false;
        }

        if (lote is { Length: > LotMaxLength } || serie is { Length: > LotMaxLength })
        {
            return false;
        }

        package = new ScannedPackage(
            Gtin.ToGtin14(gtin), lote, validade, serie, ScannedCodeKind.Gs1DataMatrix);
        return true;
    }

    /// <summary>
    /// Converte a validade no formato AAMMDD do padrão GS1.
    /// </summary>
    /// <remarks>
    /// O dia <c>00</c> é previsto pelo padrão e significa "último dia do mês" — é o caso
    /// mais comum em medicamento, cuja embalagem costuma trazer apenas mês e ano. Tratar
    /// esse zero como data inválida descartaria justamente a validade que interessa.
    /// </remarks>
    private static DateOnly? ParseValidade(string aammdd)
    {
        if (aammdd.Length != 6 || !aammdd.All(char.IsAsciiDigit))
        {
            return null;
        }

        var ano = 2000 + int.Parse(aammdd[..2]);
        var mes = int.Parse(aammdd.Substring(2, 2));
        var dia = int.Parse(aammdd.Substring(4, 2));

        if (mes is < 1 or > 12)
        {
            return null;
        }

        if (dia == 0)
        {
            dia = DateTime.DaysInMonth(ano, mes);
        }
        else if (dia > DateTime.DaysInMonth(ano, mes))
        {
            return null;
        }

        return new DateOnly(ano, mes, dia);
    }

    /// <summary>
    /// Remove o prefixo de simbologia que alguns leitores emitem antes do conteúdo.
    /// </summary>
    private static string RemoveSymbologyIdentifier(string conteudo)
    {
        // "]d2" identifica DataMatrix com FNC1; "]C1" identifica GS1-128.
        if (conteudo.StartsWith("]d2", StringComparison.Ordinal) ||
            conteudo.StartsWith("]C1", StringComparison.Ordinal) ||
            conteudo.StartsWith("]e0", StringComparison.Ordinal))
        {
            return conteudo[3..];
        }

        return conteudo;
    }

    private static string? Vazio(string valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
