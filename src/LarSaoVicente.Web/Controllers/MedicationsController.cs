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
/// Catálogo de medicamentos e insumos (US09, US23 e US24).
/// </summary>
/// <remarks>
/// O catálogo <b>se constrói pelo uso</b>: começa vazio e cresce a cada item novo que
/// passa pelo balcão. Não há consulta a serviço externo em momento algum — a leitura
/// acontece com alguém esperando, e um dia de internet ruim não pode impedir o trabalho.
/// </remarks>
[Route("Medicamentos")]
public class MedicationsController : Controller
{
    private readonly AppDbContext _context;
    private readonly IDateTimeProvider _relogio;
    private readonly ILogger<MedicationsController> _logger;

    public MedicationsController(
        AppDbContext context,
        IDateTimeProvider relogio,
        ILogger<MedicationsController> logger)
    {
        _context = context;
        _relogio = relogio;
        _logger = logger;
    }

    /// <summary>
    /// Lista o catálogo, com pesquisa por nome comercial ou princípio ativo (US24).
    /// </summary>
    /// <remarks>
    /// A busca cobre os dois campos de propósito. O mesmo medicamento de laboratórios
    /// diferentes gera registros distintos, então "o que a pessoa toma" só é respondido
    /// pelo princípio ativo — nas planilhas da instituição o mesmo item aparece ora como
    /// Macrodantina, ora como Nitrofurantoína, sem nada relacionando os dois.
    /// </remarks>
    [HttpGet("")]
    public async Task<IActionResult> Index(string? busca, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Catálogo de medicamentos";

        busca = busca?.Trim();

        var consulta = _context.Medications.AsNoTracking();

        if (!string.IsNullOrEmpty(busca))
        {
            // A chave normalizada faz "Losartana 50 mg" e "Losartana 50mg" caírem no
            // mesmo lugar: catorze itens da planilha da instituição estão gravados nas
            // duas grafias.
            var chave = SearchKey.From(busca);

            consulta = consulta.Where(m =>
                m.CommercialName.Contains(busca) ||
                (m.ActiveIngredient != null && m.ActiveIngredient.Contains(busca)) ||
                m.NormalizedSearchKey.Contains(chave));
        }

        var itens = await consulta
            // Ativos primeiro: são o foco do trabalho diário.
            .OrderBy(m => m.Status)
            .ThenBy(m => m.CommercialName)
            .ToListAsync(cancellationToken);

        var ids = itens.Select(m => m.Id).ToList();

        // O estoque é a soma das entradas conferidas. Nada se move de lugar.
        var estoque = await _context.MedicationEntries
            .AsNoTracking()
            .Where(e => ids.Contains(e.MedicationId) && e.Status == MedicationEntryStatus.Conferida)
            .GroupBy(e => e.MedicationId)
            .Select(g => new { MedicationId = g.Key, Total = g.Sum(e => e.Quantity) })
            .ToDictionaryAsync(x => x.MedicationId, x => x.Total, cancellationToken);

        var modelo = new MedicationIndexViewModel
        {
            Busca = busca,
            Itens = itens.Select(m => new MedicationListItemViewModel
            {
                Id = m.Id,
                NomeExibicao = m.GetDisplayName(),
                ActiveIngredient = m.ActiveIngredient,
                PackageUnit = m.PackageUnit,
                UnitsPerPackage = m.UnitsPerPackage,
                Category = m.Category,
                Status = m.Status,
                TemCodigoDeBarras = m.Barcode is not null,
                EmEstoque = estoque.GetValueOrDefault(m.Id)
            }).ToList(),
            // Distingue "catálogo vazio" de "a pesquisa não encontrou nada".
            SemCadastros = string.IsNullOrEmpty(busca)
                ? itens.Count == 0
                : !await _context.Medications.AnyAsync(cancellationToken)
        };

        return View(modelo);
    }

    /// <summary>Ficha do item, com as entradas registradas e o estoque calculado.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detalhes([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var item = await _context.Medications
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (item is null)
        {
            return NaoEncontrado();
        }

        var entradas = await _context.MedicationEntries
            .AsNoTracking()
            .Where(e => e.MedicationId == id)
            .OrderByDescending(e => e.ReceivedOn)
            .ThenByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        ViewData["Title"] = item.GetDisplayName();
        return View(MedicationDetailsViewModel.DeEntidade(item, entradas, _relogio));
    }

    /// <summary>
    /// Tela de leitura do código de barras ou DataMatrix (US23).
    /// </summary>
    /// <remarks>
    /// O leitor USB opera como teclado: digita o conteúdo no campo em foco e envia Enter,
    /// o que para a aplicação é um envio de formulário comum. Por isso não há driver,
    /// SDK nem JavaScript envolvido.
    /// </remarks>
    [HttpGet("Identificar")]
    public IActionResult Identificar()
    {
        ViewData["Title"] = "Identificar medicamento";
        return View(new ScanViewModel());
    }

    [HttpPost("Identificar")]
    public async Task<IActionResult> Identificar(ScanViewModel modelo, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Identificar medicamento";

        if (string.IsNullOrWhiteSpace(modelo.Codigo))
        {
            ModelState.AddModelError(nameof(modelo.Codigo), "Leia ou digite um código.");
            return View(modelo);
        }

        if (!BarcodeScan.TryParse(modelo.Codigo, out var leitura))
        {
            // Primeira camada de validação: o dígito verificador não confere, ou o
            // conteúdo não é um código reconhecível. Nada disso precisou do banco.
            ModelState.AddModelError(
                nameof(modelo.Codigo),
                "Código não reconhecido. Refaça a leitura ou use o cadastro manual.");
            return View(modelo);
        }

        var item = await _context.Medications
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Barcode == leitura.Gtin, cancellationToken);

        var validade = leitura.ExpiryDate;
        var lote = leitura.Lot;
        var serie = leitura.SerialNumber;

        if (item is null)
        {
            // Código desconhecido: abre o cadastro já preenchido, e alguém completa os
            // dados uma única vez. Da segunda caixa em diante a identificação é automática.
            TempData["Aviso"] = "Este código ainda não está no catálogo. Complete os dados uma única vez.";

            return RedirectToAction(nameof(Novo), new
            {
                codigo = leitura.Gtin,
                lote,
                validade = validade?.ToString("yyyy-MM-dd"),
                serie
            });
        }

        if (item.Status == MedicationStatus.Inativo)
        {
            TempData["Aviso"] = $"{item.GetDisplayName()} está inativo no catálogo. Reative antes de registrar entradas.";
            return RedirectToAction(nameof(Detalhes), new { id = item.Id });
        }

        _logger.LogInformation(
            "Código {Tipo} identificado para o medicamento {Id}.", leitura.Kind, item.Id);

        return RedirectToAction("Nova", "MedicationEntries", new
        {
            medicamentoId = item.Id,
            lote,
            validade = validade?.ToString("yyyy-MM-dd"),
            serie,
            porLeitura = true
        });
    }

    /// <summary>Formulário de cadastro (US09).</summary>
    [HttpGet("Novo")]
    public IActionResult Novo(string? codigo, string? lote, DateOnly? validade, string? serie)
    {
        ViewData["Title"] = "Novo item do catálogo";

        return View("Formulario", new MedicationFormViewModel
        {
            Barcode = codigo,
            LoteLido = lote,
            ValidadeLida = validade,
            SerieLida = serie,
            VeioDeLeitura = !string.IsNullOrEmpty(codigo)
        });
    }

    [HttpPost("Novo")]
    public async Task<IActionResult> Novo(MedicationFormViewModel modelo, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Novo item do catálogo";

        await ValidarFormularioAsync(modelo, idExistente: null, cancellationToken);

        if (!ModelState.IsValid)
        {
            return View("Formulario", modelo);
        }

        try
        {
            var item = new Medication(
                modelo.CommercialName,
                modelo.ActiveIngredient,
                modelo.Strength,
                modelo.Form,
                modelo.UnitsPerPackage,
                modelo.PackageUnit!.Value,
                modelo.Barcode,
                modelo.Category!.Value,
                modelo.Notes);

            _context.Medications.Add(item);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Item de catálogo {Id} cadastrado.", item.Id);
            TempData["Sucesso"] = $"{item.GetDisplayName()} foi cadastrado no catálogo.";

            // Veio do balcão, com a caixa na mão: segue direto para registrar a entrada.
            if (modelo.VeioDeLeitura)
            {
                return RedirectToAction("Nova", "MedicationEntries", new
                {
                    medicamentoId = item.Id,
                    lote = modelo.LoteLido,
                    validade = modelo.ValidadeLida?.ToString("yyyy-MM-dd"),
                    serie = modelo.SerieLida,
                    porLeitura = true
                });
            }

            return RedirectToAction(nameof(Detalhes), new { id = item.Id });
        }
        catch (DomainValidationException excecao)
        {
            ModelState.AddModelError(string.Empty, excecao.Message);
            return View("Formulario", modelo);
        }
    }

    /// <summary>Formulário de edição.</summary>
    [HttpGet("{id:guid}/Editar")]
    public async Task<IActionResult> Editar([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var item = await _context.Medications
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (item is null)
        {
            return NaoEncontrado();
        }

        ViewData["Title"] = $"Editar {item.GetDisplayName()}";
        return View("Formulario", MedicationFormViewModel.DeEntidade(item));
    }

    [HttpPost("{id:guid}/Editar")]
    public async Task<IActionResult> Editar(
        [FromRoute] Guid id,
        MedicationFormViewModel modelo,
        CancellationToken cancellationToken)
    {
        // [FromRoute] é obrigatório: sem ele o ASP.NET Core preencheria o parâmetro com
        // o campo oculto do formulário, porque o provedor de formulário tem precedência
        // sobre o de rota. Alguém poderia abrir um item e gravar sobre outro.
        modelo.Id = id;
        ViewData["Title"] = "Editar item do catálogo";

        var item = await _context.Medications.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (item is null)
        {
            return NaoEncontrado();
        }

        await ValidarFormularioAsync(modelo, idExistente: id, cancellationToken);

        if (!ModelState.IsValid)
        {
            return View("Formulario", modelo);
        }

        try
        {
            item.Update(
                modelo.CommercialName,
                modelo.ActiveIngredient,
                modelo.Strength,
                modelo.Form,
                modelo.UnitsPerPackage,
                modelo.PackageUnit!.Value,
                modelo.Barcode,
                modelo.Category!.Value,
                modelo.Notes);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Item de catálogo {Id} atualizado.", id);
            TempData["Sucesso"] = "Item atualizado com sucesso.";

            return RedirectToAction(nameof(Detalhes), new { id });
        }
        catch (DomainValidationException excecao)
        {
            ModelState.AddModelError(string.Empty, excecao.Message);
            return View("Formulario", modelo);
        }
    }

    /// <summary>
    /// Inativa o item, preservando o histórico de entradas que apontam para ele.
    /// </summary>
    [HttpPost("{id:guid}/Inativar")]
    public async Task<IActionResult> Inativar([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var item = await _context.Medications.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (item is null)
        {
            return NaoEncontrado();
        }

        item.Deactivate();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Item de catálogo {Id} inativado.", id);
        TempData["Sucesso"] = $"{item.GetDisplayName()} foi inativado. O histórico foi preservado.";

        return RedirectToAction(nameof(Detalhes), new { id });
    }

    [HttpPost("{id:guid}/Reativar")]
    public async Task<IActionResult> Reativar([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var item = await _context.Medications.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (item is null)
        {
            return NaoEncontrado();
        }

        item.Reactivate();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Item de catálogo {Id} reativado.", id);
        TempData["Sucesso"] = $"{item.GetDisplayName()} foi reativado.";

        return RedirectToAction(nameof(Detalhes), new { id });
    }

    /// <summary>
    /// Validações que dependem do banco ou da combinação de campos.
    /// </summary>
    /// <remarks>
    /// Ficam aqui, e não em atributos, porque a obrigatoriedade do princípio ativo
    /// depende da categoria e a unicidade do código depende de uma consulta.
    /// </remarks>
    private async Task ValidarFormularioAsync(
        MedicationFormViewModel modelo,
        Guid? idExistente,
        CancellationToken cancellationToken)
    {
        if (modelo.Category == MedicationCategory.Medicamento)
        {
            if (string.IsNullOrWhiteSpace(modelo.ActiveIngredient))
            {
                ModelState.AddModelError(
                    nameof(modelo.ActiveIngredient),
                    "Informe o princípio ativo. É ele que liga o mesmo remédio entre marcas diferentes.");
            }

            if (modelo.Form is null)
            {
                ModelState.AddModelError(
                    nameof(modelo.Form),
                    "Selecione a forma farmacêutica.");
            }
        }

        var codigo = Gtin.Normalize(modelo.Barcode);

        if (codigo is not null)
        {
            if (!Gtin.IsValid(codigo))
            {
                ModelState.AddModelError(
                    nameof(modelo.Barcode),
                    "Código de barras inválido. Confira os dígitos ou refaça a leitura.");
            }
            else
            {
                var canonico = Gtin.ToGtin14(codigo);

                var jaExiste = await _context.Medications
                    .AsNoTracking()
                    .Where(m => m.Barcode == canonico)
                    .Where(m => idExistente == null || m.Id != idExistente)
                    .Select(m => new { m.Id, m.CommercialName })
                    .FirstOrDefaultAsync(cancellationToken);

                if (jaExiste is not null)
                {
                    // Melhor avisar aqui do que deixar o banco recusar com uma mensagem
                    // que o funcionário não teria como interpretar.
                    ModelState.AddModelError(
                        nameof(modelo.Barcode),
                        $"Este código já está cadastrado para \"{jaExiste.CommercialName}\".");
                }
            }
        }
    }

    private IActionResult NaoEncontrado()
    {
        TempData["Erro"] = "Item não encontrado no catálogo.";
        return RedirectToAction(nameof(Index));
    }
}
