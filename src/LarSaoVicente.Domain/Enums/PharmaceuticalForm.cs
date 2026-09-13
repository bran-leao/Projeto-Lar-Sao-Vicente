using System.ComponentModel.DataAnnotations;

namespace LarSaoVicente.Domain.Enums;

/// <summary>
/// Forma farmacêutica do medicamento.
/// </summary>
/// <remarks>
/// Os valores foram extraídos das 144 linhas da planilha de catálogo da instituição, e
/// não de uma lista teórica: comprimido (126 itens), colírio, gotas, sachê e inalador
/// aparecem no dado real. É lista fechada porque texto livre reproduziria no banco o
/// problema das planilhas, em que "CX", "Cx" e "caixa" são três registros distintos
/// para a máquina e o mesmo para a pessoa.
/// </remarks>
public enum PharmaceuticalForm
{
    [Display(Name = "Comprimido")]
    Comprimido = 1,

    [Display(Name = "Cápsula")]
    Capsula = 2,

    [Display(Name = "Sachê")]
    Sache = 3,

    /// <summary>Solução de uso oral, incluindo xaropes e gotas.</summary>
    [Display(Name = "Solução oral")]
    SolucaoOral = 4,

    [Display(Name = "Colírio")]
    Colirio = 5,

    [Display(Name = "Pomada ou creme")]
    PomadaCreme = 6,

    [Display(Name = "Injetável")]
    Injetavel = 7,

    [Display(Name = "Inalador")]
    Inalador = 8,

    [Display(Name = "Outra")]
    Outra = 99
}
