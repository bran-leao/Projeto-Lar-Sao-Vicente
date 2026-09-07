using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace LarSaoVicente.Web.Extensions;

/// <summary>Auxílios de apresentação para enumerações.</summary>
public static class EnumExtensions
{
    /// <summary>
    /// Retorna o texto definido em <see cref="DisplayAttribute"/>, ou o próprio nome do
    /// membro quando o atributo não existir.
    /// </summary>
    /// <remarks>
    /// Evita repetir uma tradução manual de "GrauI" para "Grau I" em cada view.
    /// </remarks>
    public static string ObterNomeExibicao(this Enum valor)
    {
        var membro = valor.GetType().GetMember(valor.ToString()).FirstOrDefault();
        var atributo = membro?.GetCustomAttribute<DisplayAttribute>();

        return atributo?.Name ?? valor.ToString();
    }
}
