using LarSaoVicente.Domain.Abstractions;
using LarSaoVicente.Domain.Common;
using LarSaoVicente.Domain.Entities;
using LarSaoVicente.Domain.Enums;
using LarSaoVicente.Infrastructure.Persistence;
using LarSaoVicente.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LarSaoVicente.Web.Controllers;

/// <summary>
/// Cadastro e manutenção dos residentes da instituição (US02 a US06).
/// </summary>
/// <remarks>
/// O controller coordena a operação, mas não decide as regras: quem valida os dados é a
/// entidade <see cref="Resident"/>. Aqui apenas traduzimos o resultado dessa validação
/// em mensagens para o funcionário.
/// </remarks>
[Route("Residentes")]
public class ResidentsController : Controller
{
    private readonly AppDbContext _context;
    private readonly IDateTimeProvider _relogio;
    private readonly ILogger<ResidentsController> _logger;

    public ResidentsController(
        AppDbContext context,
        IDateTimeProvider relogio,
        ILogger<ResidentsController> logger)
    {
        _context = context;
        _relogio = relogio;
        _logger = logger;
    }

    /// <summary>Lista os residentes, com pesquisa opcional por nome (US03).</summary>
    [HttpGet("")]
    public async Task<IActionResult> Index(string? busca, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Residentes";

        busca = busca?.Trim();

        var consulta = _context.Residents.AsNoTracking();

        if (!string.IsNullOrEmpty(busca))
        {
            // Comparação sem diferenciar maiúsculas: o SQL Server usa collation
            // que já ignora caixa, e o EF traduz a chamada para um LIKE.
            consulta = consulta.Where(r => r.FullName.Contains(busca));
        }

        var residentes = await consulta
            // Ativos primeiro: são o foco do trabalho diário da equipe.
            .OrderBy(r => r.Status)
            .ThenBy(r => r.FullName)
            .ToListAsync(cancellationToken);

        var hoje = _relogio.Today;

        var modelo = new ResidentIndexViewModel
        {
            Busca = busca,
            Residentes = residentes.Select(r => new ResidentListItemViewModel
            {
                Id = r.Id,
                FullName = r.FullName,
                Idade = r.GetAgeOn(hoje),
                Room = r.Room,
                DependencyLevel = r.DependencyLevel,
                Status = r.Status
            }).ToList(),
            // Distingue "nenhum residente cadastrado" de "a pesquisa não encontrou nada",
            // situações que pedem mensagens diferentes na tela.
            SemCadastros = string.IsNullOrEmpty(busca)
                ? residentes.Count == 0
                : !await _context.Residents.AnyAsync(cancellationToken)
        };

        return View(modelo);
    }

    /// <summary>Perfil do residente (US04).</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detalhes([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var residente = await _context.Residents
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (residente is null)
        {
            return ResidenteNaoEncontrado();
        }

        ViewData["Title"] = residente.FullName;
        return View(ResidentDetailsViewModel.DeEntidade(residente, _relogio));
    }

    /// <summary>Formulário de cadastro (US02).</summary>
    [HttpGet("Novo")]
    public IActionResult Novo()
    {
        ViewData["Title"] = "Novo residente";
        return View("Formulario", new ResidentFormViewModel());
    }

    [HttpPost("Novo")]
    public async Task<IActionResult> Novo(ResidentFormViewModel modelo, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Novo residente";

        ValidarDatas(modelo);

        if (!ModelState.IsValid)
        {
            return View("Formulario", modelo);
        }

        try
        {
            var residente = new Resident(
                modelo.FullName,
                modelo.BirthDate!.Value,
                modelo.AdmissionDate!.Value,
                modelo.Room,
                modelo.DependencyLevel!.Value,
                modelo.Notes,
                _relogio.Today);

            _context.Residents.Add(residente);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Residente {Id} cadastrado.", residente.Id);
            TempData["Sucesso"] = $"Residente {residente.FullName} cadastrado com sucesso.";

            return RedirectToAction(nameof(Detalhes), new { id = residente.Id });
        }
        catch (DomainValidationException excecao)
        {
            // Rede de segurança: o domínio recusou algo que a validação do formulário
            // deixou passar. A mensagem é exibida no topo do formulário.
            ModelState.AddModelError(string.Empty, excecao.Message);
            return View("Formulario", modelo);
        }
    }

    /// <summary>Formulário de edição (US05).</summary>
    [HttpGet("{id:guid}/Editar")]
    public async Task<IActionResult> Editar([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var residente = await _context.Residents
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (residente is null)
        {
            return ResidenteNaoEncontrado();
        }

        ViewData["Title"] = $"Editar {residente.FullName}";
        return View("Formulario", ResidentFormViewModel.DeEntidade(residente));
    }

    [HttpPost("{id:guid}/Editar")]
    public async Task<IActionResult> Editar(
        [FromRoute] Guid id,
        ResidentFormViewModel modelo,
        CancellationToken cancellationToken)
    {
        // [FromRoute] é obrigatório aqui. Sem ele o ASP.NET Core preenche o parâmetro
        // com o campo "Id" enviado no formulário, porque o provedor de valores de
        // formulário tem precedência sobre o de rota. Um usuário mal-intencionado
        // poderia então abrir o próprio cadastro, trocar esse campo oculto e gravar
        // sobre o registro de outro residente.
        modelo.Id = id;
        ViewData["Title"] = "Editar residente";

        var residente = await _context.Residents.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (residente is null)
        {
            return ResidenteNaoEncontrado();
        }

        ValidarDatas(modelo);

        if (!ModelState.IsValid)
        {
            return View("Formulario", modelo);
        }

        try
        {
            residente.Update(
                modelo.FullName,
                modelo.BirthDate!.Value,
                modelo.AdmissionDate!.Value,
                modelo.Room,
                modelo.DependencyLevel!.Value,
                modelo.Notes,
                _relogio.Today);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Residente {Id} atualizado.", residente.Id);
            TempData["Sucesso"] = "Cadastro atualizado com sucesso.";

            return RedirectToAction(nameof(Detalhes), new { id });
        }
        catch (DomainValidationException excecao)
        {
            ModelState.AddModelError(string.Empty, excecao.Message);
            return View("Formulario", modelo);
        }
    }

    /// <summary>
    /// Inativa o residente, preservando o cadastro e todo o histórico (US06).
    /// </summary>
    [HttpPost("{id:guid}/Inativar")]
    public async Task<IActionResult> Inativar([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var residente = await _context.Residents.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (residente is null)
        {
            return ResidenteNaoEncontrado();
        }

        residente.Deactivate();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Residente {Id} inativado.", id);
        TempData["Sucesso"] = $"{residente.FullName} foi inativado. O histórico foi preservado.";

        return RedirectToAction(nameof(Detalhes), new { id });
    }

    /// <summary>
    /// Reativa um residente inativado.
    /// </summary>
    /// <remarks>
    /// Contrapartida direta da inativação: sem ela, uma inativação feita por engano
    /// não teria como ser desfeita pela própria interface.
    /// </remarks>
    [HttpPost("{id:guid}/Reativar")]
    public async Task<IActionResult> Reativar([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var residente = await _context.Residents.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (residente is null)
        {
            return ResidenteNaoEncontrado();
        }

        residente.Reactivate();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Residente {Id} reativado.", id);
        TempData["Sucesso"] = $"{residente.FullName} foi reativado.";

        return RedirectToAction(nameof(Detalhes), new { id });
    }

    /// <summary>
    /// Valida as regras de data que dependem do dia corrente.
    /// </summary>
    /// <remarks>
    /// Fica no controller, e não em um atributo de validação, porque depende de
    /// <see cref="IDateTimeProvider"/>. As mensagens são associadas ao respectivo campo
    /// para aparecerem ao lado dele no formulário.
    /// </remarks>
    private void ValidarDatas(ResidentFormViewModel modelo)
    {
        var hoje = _relogio.Today;

        if (modelo.BirthDate is { } nascimento && nascimento > hoje)
        {
            ModelState.AddModelError(
                nameof(modelo.BirthDate),
                "A data de nascimento não pode estar no futuro.");
        }

        if (modelo.AdmissionDate is { } entrada)
        {
            if (entrada > hoje)
            {
                ModelState.AddModelError(
                    nameof(modelo.AdmissionDate),
                    "A data de entrada não pode estar no futuro.");
            }

            if (modelo.BirthDate is { } dataNascimento && entrada < dataNascimento)
            {
                ModelState.AddModelError(
                    nameof(modelo.AdmissionDate),
                    "A data de entrada não pode ser anterior à data de nascimento.");
            }
        }
    }

    private IActionResult ResidenteNaoEncontrado()
    {
        TempData["Erro"] = "Residente não encontrado.";
        return RedirectToAction(nameof(Index));
    }
}
