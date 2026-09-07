using LarSaoVicente.Domain.Enums;
using LarSaoVicente.Infrastructure.Persistence;
using LarSaoVicente.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LarSaoVicente.Web.Controllers;

/// <summary>
/// Tela inicial com o resumo da instituição (US07).
/// </summary>
public class DashboardController : Controller
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Dashboard";

        // Uma única consulta agrupada evita seis idas ao banco para montar a tela.
        var agrupamento = await _context.Residents
            .AsNoTracking()
            .GroupBy(r => new { r.Status, r.DependencyLevel })
            .Select(g => new
            {
                g.Key.Status,
                g.Key.DependencyLevel,
                Quantidade = g.Count()
            })
            .ToListAsync(cancellationToken);

        int ContarAtivos(DependencyLevel grau) => agrupamento
            .Where(x => x.Status == ResidentStatus.Ativo && x.DependencyLevel == grau)
            .Sum(x => x.Quantidade);

        int ContarPorSituacao(ResidentStatus situacao) => agrupamento
            .Where(x => x.Status == situacao)
            .Sum(x => x.Quantidade);

        var modelo = new DashboardViewModel
        {
            Total = agrupamento.Sum(x => x.Quantidade),
            Ativos = ContarPorSituacao(ResidentStatus.Ativo),
            Inativos = ContarPorSituacao(ResidentStatus.Inativo),
            AtivosGrauI = ContarAtivos(DependencyLevel.GrauI),
            AtivosGrauII = ContarAtivos(DependencyLevel.GrauII),
            AtivosGrauIII = ContarAtivos(DependencyLevel.GrauIII)
        };

        return View(modelo);
    }
}
