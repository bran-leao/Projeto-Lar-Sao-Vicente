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
/// Registro das remessas recebidas e sua conferência (US10, US26 e US27).
/// </summary>
/// <remarks>
/// O estoque é a soma das entradas conferidas — nada é transferido de um lugar para
/// outro. Uma entrada pendente simplesmente não é contada, e por isso não existe
/// operação de transferência que possa falhar pela metade ou ser esquecida.
/// </remarks>
[Route("Entradas")]
public class MedicationEntriesController : Controller
{
    private readonly AppDbContext _context;
    private readonly IDateTimeProvider _relogio;
    private readonly ICurrentUser _usuarioAtual;
    private readonly ILogger<MedicationEntriesController> _logger;

    public MedicationEntriesController(
        AppDbContext context,
        IDateTimeProvider relogio,
        ICurrentUser usuarioAtual,
        ILogger<MedicationEntriesController> logger)
    {
        _context = context;
        _relogio = relogio;
        _usuarioAtual = usuarioAtual;
        _logger = logger;
    }

    /// <summary>Entradas aguardando conferência (US27).</summary>
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Conferência de entradas";

        var pendentes = await _context.MedicationEntries
            .AsNoTracking()
            .Include(e => e.Medication)
            .Where(e => e.Status == MedicationEntryStatus.AguardandoConferencia)
            // Validade desconhecida e lote vencido primeiro: são o risco maior.
            .OrderBy(e => e.ExpiryDate ?? DateOnly.MinValue)
            .ThenByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        var hoje = _relogio.Today;

        return View(pendentes
            .Select(e => MedicationEntryListItemViewModel.DeEntidade(e, hoje))
            .ToList());
    }

    /// <summary>Formulário de registro de uma remessa (US10 e US26).</summary>
    [HttpGet("Nova")]
    public async Task<IActionResult> Nova(
        Guid medicamentoId,
        string? lote,
        DateOnly? validade,
        string? serie,
        bool porLeitura,
        CancellationToken cancellationToken)
    {
        var item = await _context.Medications
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == medicamentoId, cancellationToken);

        if (item is null)
        {
            TempData["Erro"] = "Escolha no catálogo o item que chegou.";
            return RedirectToAction("Index", "Medications");
        }

        ViewData["Title"] = $"Entrada de {item.GetDisplayName()}";

        return View("Formulario", new MedicationEntryFormViewModel
        {
            MedicationId = item.Id,
            NomeDoMedicamento = item.GetDisplayName(),
            UnidadeDoMedicamento = item.PackageUnit,
            Quantity = 1,
            LotNumber = lote,
            ExpiryDate = validade,
            // A caixa trouxe DataMatrix sem validade legível? A marcação fica a cargo de
            // quem registra — nunca preenchida sozinha.
            SerialNumber = serie,
            Origin = null,
            ReceivedOn = _relogio.Today,
            IdentifiedByScan = porLeitura
        });
    }

    [HttpPost("Nova")]
    public async Task<IActionResult> Nova(
        MedicationEntryFormViewModel modelo,
        CancellationToken cancellationToken)
    {
        var item = await _context.Medications
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == modelo.MedicationId, cancellationToken);

        if (item is null)
        {
            TempData["Erro"] = "Escolha no catálogo o item que chegou.";
            return RedirectToAction("Index", "Medications");
        }

        modelo.NomeDoMedicamento = item.GetDisplayName();
        modelo.UnidadeDoMedicamento = item.PackageUnit;
        ViewData["Title"] = $"Entrada de {item.GetDisplayName()}";

        ValidarValidade(modelo);

        if (!ModelState.IsValid)
        {
            return View("Formulario", modelo);
        }

        try
        {
            var entrada = new MedicationEntry(
                modelo.MedicationId,
                modelo.Quantity!.Value,
                modelo.LotNumber,
                modelo.ExpiryNotIdentified ? null : modelo.ExpiryDate,
                modelo.ExpiryNotIdentified,
                modelo.Origin!.Value,
                modelo.ReceivedOn!.Value,
                modelo.IdentifiedByScan,
                modelo.SerialNumber,
                modelo.Notes,
                _relogio.Today);

            _context.MedicationEntries.Add(entrada);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Entrada {Id} registrada para o medicamento {MedicamentoId}, quantidade {Quantidade}.",
                entrada.Id, entrada.MedicationId, entrada.Quantity);

            var unidade = item.PackageUnit.ToString().ToLowerInvariant();
            TempData["Sucesso"] =
                $"{entrada.Quantity} {unidade}(s) de {item.GetDisplayName()} registrada(s). " +
                "A entrada aguarda conferência e ainda não conta no estoque.";

            return RedirectToAction("Detalhes", "Medications", new { id = entrada.MedicationId });
        }
        catch (DomainValidationException excecao)
        {
            ModelState.AddModelError(string.Empty, excecao.Message);
            return View("Formulario", modelo);
        }
    }

    /// <summary>Corrige uma entrada ainda pendente.</summary>
    [HttpGet("{id:guid}/Editar")]
    public async Task<IActionResult> Editar([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var entrada = await _context.MedicationEntries
            .AsNoTracking()
            .Include(e => e.Medication)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (entrada?.Medication is null)
        {
            return NaoEncontrada();
        }

        if (entrada.Status != MedicationEntryStatus.AguardandoConferencia)
        {
            TempData["Erro"] = "Só é possível alterar uma entrada que ainda aguarda conferência.";
            return RedirectToAction("Detalhes", "Medications", new { id = entrada.MedicationId });
        }

        ViewData["Title"] = $"Editar entrada de {entrada.Medication.GetDisplayName()}";
        return View("Formulario", MedicationEntryFormViewModel.DeEntidade(entrada, entrada.Medication));
    }

    [HttpPost("{id:guid}/Editar")]
    public async Task<IActionResult> Editar(
        [FromRoute] Guid id,
        MedicationEntryFormViewModel modelo,
        CancellationToken cancellationToken)
    {
        // [FromRoute] pelo mesmo motivo do cadastro de residentes: o provedor de
        // formulário tem precedência sobre o de rota, e sem a marcação um campo oculto
        // alterado no navegador gravaria sobre outra entrada.
        modelo.Id = id;

        var entrada = await _context.MedicationEntries
            .Include(e => e.Medication)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (entrada?.Medication is null)
        {
            return NaoEncontrada();
        }

        // Mesma guarda do GET: devolver o formulário aqui deixaria o funcionário
        // reenviando um dado que nunca será aceito, sem entender por quê.
        if (entrada.Status != MedicationEntryStatus.AguardandoConferencia)
        {
            TempData["Erro"] = "Só é possível alterar uma entrada que ainda aguarda conferência.";
            return RedirectToAction("Detalhes", "Medications", new { id = entrada.MedicationId });
        }

        modelo.MedicationId = entrada.MedicationId;
        modelo.NomeDoMedicamento = entrada.Medication.GetDisplayName();
        modelo.UnidadeDoMedicamento = entrada.Medication.PackageUnit;
        ViewData["Title"] = "Editar entrada";

        ValidarValidade(modelo);

        if (!ModelState.IsValid)
        {
            return View("Formulario", modelo);
        }

        try
        {
            entrada.Update(
                modelo.Quantity!.Value,
                modelo.LotNumber,
                modelo.ExpiryNotIdentified ? null : modelo.ExpiryDate,
                modelo.ExpiryNotIdentified,
                modelo.Origin!.Value,
                modelo.ReceivedOn!.Value,
                modelo.SerialNumber,
                modelo.Notes,
                _relogio.Today);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Entrada {Id} atualizada.", id);
            TempData["Sucesso"] = "Entrada atualizada com sucesso.";

            return RedirectToAction("Detalhes", "Medications", new { id = entrada.MedicationId });
        }
        catch (DomainValidationException excecao)
        {
            ModelState.AddModelError(string.Empty, excecao.Message);
            return View("Formulario", modelo);
        }
    }

    /// <summary>
    /// Libera a entrada para o estoque (US27).
    /// </summary>
    /// <remarks>
    /// Enquanto não houver perfis de acesso (US08), qualquer usuário autenticado pode
    /// conferir. A restrição entra junto com a US08: liberar medicamento para uso é
    /// decisão de responsabilidade técnica, não operacional.
    /// </remarks>
    [HttpPost("{id:guid}/Conferir")]
    public async Task<IActionResult> Conferir([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var entrada = await _context.MedicationEntries
            .Include(e => e.Medication)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (entrada is null)
        {
            return NaoEncontrada();
        }

        try
        {
            entrada.Confirm(_usuarioAtual.UserId, _relogio.UtcNow);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Entrada {Id} conferida e liberada para o estoque.", id);
            TempData["Sucesso"] =
                $"{entrada.Quantity} unidade(s) de {entrada.Medication?.GetDisplayName()} liberada(s) para o estoque.";
        }
        catch (DomainValidationException excecao)
        {
            TempData["Erro"] = excecao.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Recusa a entrada, com o motivo registrado.
    /// </summary>
    /// <remarks>
    /// A entrada recusada permanece gravada: é histórico e é informação de gestão —
    /// permite responder quantas doações vencidas a instituição recebeu em um período.
    /// </remarks>
    [HttpPost("{id:guid}/Recusar")]
    public async Task<IActionResult> Recusar(
        [FromRoute] Guid id,
        string? motivo,
        CancellationToken cancellationToken)
    {
        var entrada = await _context.MedicationEntries
            .Include(e => e.Medication)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (entrada is null)
        {
            return NaoEncontrada();
        }

        try
        {
            entrada.Reject(motivo ?? string.Empty, _usuarioAtual.UserId, _relogio.UtcNow);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Entrada {Id} recusada.", id);
            TempData["Sucesso"] = $"Entrada de {entrada.Medication?.GetDisplayName()} recusada e registrada.";
        }
        catch (DomainValidationException excecao)
        {
            TempData["Erro"] = excecao.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Exige que a validade seja informada ou explicitamente marcada como desconhecida.
    /// </summary>
    /// <remarks>
    /// Deixar em branco não pode ser o caminho fácil: validade desconhecida é o principal
    /// risco em medicamento doado e precisa aparecer para quem confere, não sumir.
    /// </remarks>
    private void ValidarValidade(MedicationEntryFormViewModel modelo)
    {
        if (modelo.ExpiryNotIdentified && modelo.ExpiryDate is not null)
        {
            ModelState.AddModelError(
                nameof(modelo.ExpiryDate),
                "Informe a validade ou marque que ela não pôde ser identificada — não as duas coisas.");
        }

        if (!modelo.ExpiryNotIdentified && modelo.ExpiryDate is null)
        {
            ModelState.AddModelError(
                nameof(modelo.ExpiryDate),
                "Informe a validade ou marque que ela não pôde ser identificada na embalagem.");
        }

        if (modelo.ReceivedOn is { } recebimento && recebimento > _relogio.Today)
        {
            ModelState.AddModelError(
                nameof(modelo.ReceivedOn),
                "A data de recebimento não pode estar no futuro.");
        }
    }

    private IActionResult NaoEncontrada()
    {
        TempData["Erro"] = "Entrada não encontrada.";
        return RedirectToAction(nameof(Index));
    }
}
