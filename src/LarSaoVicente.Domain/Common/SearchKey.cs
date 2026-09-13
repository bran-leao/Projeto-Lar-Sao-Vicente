using System.Globalization;
using System.Text;

namespace LarSaoVicente.Domain.Common;

/// <summary>
/// Reduz um texto à forma comparável usada nas buscas e na detecção de duplicatas.
/// </summary>
/// <remarks>
/// Existe por causa de um achado concreto no dado da instituição: catorze itens estão
/// registrados em duas grafias que diferem apenas por um espaço ou um ponto —
/// "Losartana 50 mg" e "Losartana 50mg", "Vitamina D 7.000 UI" e "Vitamina D 7000 UI".
/// Para a pessoa é o mesmo remédio; para a planilha, são linhas independentes.
/// <para>
/// A chave descarta acentos, maiúsculas, espaços e pontuação, de modo que as duas
/// grafias colidam. Ela <b>não</b> é única no banco: o mesmo medicamento de
/// laboratórios diferentes gera registros distintos, o que é correto para
/// rastreabilidade. Serve para localizar e para avisar sobre um provável duplicado,
/// nunca para impedir o cadastro.
/// </para>
/// </remarks>
public static class SearchKey
{
    public const int MaxLength = 300;

    /// <summary>Monta a chave a partir das partes informadas, ignorando as vazias.</summary>
    public static string From(params string?[] parts)
    {
        var texto = string.Concat(parts.Where(p => !string.IsNullOrWhiteSpace(p)));
        var semAcento = RemoveDiacritics(texto);

        var chave = new StringBuilder(semAcento.Length);
        foreach (var caractere in semAcento)
        {
            if (char.IsLetterOrDigit(caractere))
            {
                chave.Append(char.ToUpperInvariant(caractere));
            }
        }

        return chave.Length > MaxLength ? chave.ToString(0, MaxLength) : chave.ToString();
    }

    private static string RemoveDiacritics(string texto)
    {
        var decomposto = texto.Normalize(NormalizationForm.FormD);
        var resultado = new StringBuilder(decomposto.Length);

        foreach (var caractere in decomposto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caractere) != UnicodeCategory.NonSpacingMark)
            {
                resultado.Append(caractere);
            }
        }

        return resultado.ToString().Normalize(NormalizationForm.FormC);
    }
}
