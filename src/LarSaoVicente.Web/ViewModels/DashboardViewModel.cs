namespace LarSaoVicente.Web.ViewModels;

/// <summary>
/// Indicadores exibidos na tela inicial (US07).
/// </summary>
/// <remarks>
/// Todos os números vêm de contagens reais do banco. Nenhum valor é estimado ou
/// preenchido artificialmente: um indicador inventado levaria a instituição a tomar
/// decisões com base em informação falsa.
/// </remarks>
public class DashboardViewModel
{
    /// <summary>Total de residentes cadastrados, incluindo os inativos.</summary>
    public int Total { get; init; }

    public int Ativos { get; init; }

    public int Inativos { get; init; }

    /// <summary>Residentes ativos com grau de dependência I.</summary>
    public int AtivosGrauI { get; init; }

    /// <summary>Residentes ativos com grau de dependência II.</summary>
    public int AtivosGrauII { get; init; }

    /// <summary>Residentes ativos com grau de dependência III.</summary>
    public int AtivosGrauIII { get; init; }

    /// <summary>Indica que ainda não existe nenhum residente cadastrado.</summary>
    public bool SemRegistros => Total == 0;
}
