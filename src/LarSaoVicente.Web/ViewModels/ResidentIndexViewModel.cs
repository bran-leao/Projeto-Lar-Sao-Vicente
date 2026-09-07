namespace LarSaoVicente.Web.ViewModels;

/// <summary>Tela de listagem de residentes com pesquisa por nome (US03).</summary>
public class ResidentIndexViewModel
{
    /// <summary>Texto pesquisado, preservado para continuar exibido no campo de busca.</summary>
    public string? Busca { get; init; }

    public IReadOnlyList<ResidentListItemViewModel> Residentes { get; init; } = [];

    /// <summary>Verdadeiro quando não há nenhum residente cadastrado no sistema.</summary>
    public bool SemCadastros { get; init; }

    /// <summary>Verdadeiro quando existem residentes, mas nenhum corresponde à pesquisa.</summary>
    public bool BuscaSemResultado => Residentes.Count == 0 && !SemCadastros;
}
