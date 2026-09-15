using LarSaoVicente.Domain.Common;
using LarSaoVicente.Domain.Enums;

namespace LarSaoVicente.Domain.Entities;

/// <summary>
/// Uma remessa que chegou à instituição: quanto, de que lote, com que validade e de
/// que origem (US10 e US26).
/// </summary>
/// <remarks>
/// É a segunda das quatro entidades do fluxo de medicamentos. O catálogo
/// (<see cref="Medication"/>) diz <i>o que é</i>; a entrada diz <i>o que chegou</i>.
/// <para>
/// <b>A quantidade pertence aqui, nunca ao catálogo.</b> Se chegam onze envelopes de
/// dipirona, é uma entrada com quantidade onze — não onze registros. As unidades
/// compartilham lote e validade porque vieram juntas; envelopes de lotes diferentes são
/// entradas diferentes, o que é correto e é justamente o que um registro por unidade não
/// representaria melhor.
/// </para>
/// </remarks>
public class MedicationEntry : AuditableEntity
{
    public const int LotNumberMaxLength = 20;
    public const int SerialNumberMaxLength = 20;
    public const int RejectionReasonMaxLength = 500;
    public const int NotesMaxLength = 1000;

    /// <summary>Maior quantidade aceita em uma única entrada, para barrar erro de digitação.</summary>
    public const int MaxQuantity = 100_000;

    public Guid Id { get; private set; }

    /// <summary>Item do catálogo que chegou.</summary>
    public Guid MedicationId { get; private set; }

    public Medication? Medication { get; private set; }

    /// <summary>Quantas embalagens chegaram, na unidade definida no catálogo.</summary>
    public int Quantity { get; private set; }

    public string? LotNumber { get; private set; }

    /// <summary>Validade impressa na embalagem. Nula quando não foi possível identificá-la.</summary>
    public DateOnly? ExpiryDate { get; private set; }

    /// <summary>
    /// Assinala que a validade não pôde ser lida na embalagem.
    /// </summary>
    /// <remarks>
    /// Cartela avulsa costuma chegar sem validade legível. Recusar o cadastro nesse caso
    /// deixaria justamente o caso mais frequente em doação fora do controle — o oposto do
    /// que a instituição pediu. O registro é aceito, mas fica marcado para quem confere:
    /// validade desconhecida é o principal risco em medicamento doado.
    /// </remarks>
    public bool ExpiryNotIdentified { get; private set; }

    public MedicationOrigin Origin { get; private set; }

    public DateOnly ReceivedOn { get; private set; }

    /// <summary>
    /// Indica que os dados vieram da leitura de um código, e não da digitação.
    /// </summary>
    /// <remarks>
    /// É o que distingue a entrada verificada pela máquina da entrada digitada por
    /// alguém, e sustenta a regra de conferência obrigatória do registro manual.
    /// </remarks>
    public bool IdentifiedByScan { get; private set; }

    /// <summary>Número de série da caixa, quando o DataMatrix o trouxer.</summary>
    public string? SerialNumber { get; private set; }

    public MedicationEntryStatus Status { get; private set; }

    /// <summary>Motivo da recusa. Obrigatório quando a entrada é recusada.</summary>
    public string? RejectionReason { get; private set; }

    /// <summary>Momento da conferência, em UTC. Nulo enquanto a entrada está pendente.</summary>
    public DateTime? ReviewedAt { get; private set; }

    /// <summary>Quem conferiu ou recusou.</summary>
    /// <remarks>
    /// Guardado explicitamente, e não deduzido dos campos de auditoria: liberar
    /// medicamento para uso é ato de responsabilidade técnica, e o registro de quem o
    /// praticou não deve se confundir com "quem alterou o registro por último".
    /// </remarks>
    public string? ReviewedByUserId { get; private set; }

    public string? Notes { get; private set; }

    /// <summary>Construtor exigido pelo Entity Framework Core para materializar a entidade.</summary>
    private MedicationEntry()
    {
    }

    /// <summary>
    /// Registra uma remessa recebida. Nasce sempre
    /// <see cref="MedicationEntryStatus.AguardandoConferencia"/>.
    /// </summary>
    /// <exception cref="DomainValidationException">Quando alguma regra de negócio é violada.</exception>
    public MedicationEntry(
        Guid medicationId,
        int quantity,
        string? lotNumber,
        DateOnly? expiryDate,
        bool expiryNotIdentified,
        MedicationOrigin origin,
        DateOnly receivedOn,
        bool identifiedByScan,
        string? serialNumber,
        string? notes,
        DateOnly today)
    {
        if (medicationId == Guid.Empty)
        {
            throw new DomainValidationException("O medicamento é obrigatório.");
        }

        Id = Guid.NewGuid();
        MedicationId = medicationId;
        IdentifiedByScan = identifiedByScan;
        Status = MedicationEntryStatus.AguardandoConferencia;

        SetData(quantity, lotNumber, expiryDate, expiryNotIdentified, origin, receivedOn, serialNumber, notes, today);
    }

    /// <summary>
    /// Corrige os dados de uma entrada ainda pendente.
    /// </summary>
    /// <remarks>
    /// Só vale enquanto a entrada aguarda conferência. Depois de conferida, o item já
    /// conta no estoque, e mudá-lo em silêncio faria o número do sistema divergir da
    /// prateleira sem deixar rastro.
    /// </remarks>
    /// <exception cref="DomainValidationException">Quando a entrada já foi revisada.</exception>
    public void Update(
        int quantity,
        string? lotNumber,
        DateOnly? expiryDate,
        bool expiryNotIdentified,
        MedicationOrigin origin,
        DateOnly receivedOn,
        string? serialNumber,
        string? notes,
        DateOnly today)
    {
        GarantirPendente("Só é possível alterar uma entrada que ainda aguarda conferência.");
        SetData(quantity, lotNumber, expiryDate, expiryNotIdentified, origin, receivedOn, serialNumber, notes, today);
    }

    /// <summary>
    /// Ajusta apenas a quantidade, mantendo o restante.
    /// </summary>
    /// <remarks>
    /// Quem registra pode ter contado errado, e quem confere precisa poder corrigir para
    /// o que realmente existe na prateleira antes de liberar.
    /// </remarks>
    /// <exception cref="DomainValidationException">Quando a entrada já foi revisada ou a quantidade é inválida.</exception>
    public void ChangeQuantity(int quantity)
    {
        GarantirPendente("Só é possível alterar a quantidade de uma entrada que ainda aguarda conferência.");
        ValidarQuantidade(quantity);
        Quantity = quantity;
    }

    /// <summary>
    /// Libera a entrada para o estoque.
    /// </summary>
    /// <exception cref="DomainValidationException">Quando a entrada já foi revisada.</exception>
    public void Confirm(string? reviewedByUserId, DateTime reviewedAtUtc)
    {
        GarantirPendente("Esta entrada já foi revisada.");

        Status = MedicationEntryStatus.Conferida;
        RejectionReason = null;
        ReviewedByUserId = reviewedByUserId;
        ReviewedAt = reviewedAtUtc;
    }

    /// <summary>
    /// Recusa a entrada, registrando o motivo.
    /// </summary>
    /// <remarks>
    /// A entrada recusada não é apagada: é histórico e é informação de gestão.
    /// </remarks>
    /// <exception cref="DomainValidationException">Quando a entrada já foi revisada ou falta o motivo.</exception>
    public void Reject(string reason, string? reviewedByUserId, DateTime reviewedAtUtc)
    {
        GarantirPendente("Esta entrada já foi revisada.");

        reason = Normalize(reason) ?? string.Empty;

        if (reason.Length == 0)
        {
            throw new DomainValidationException("O motivo da recusa é obrigatório.");
        }

        if (reason.Length > RejectionReasonMaxLength)
        {
            throw new DomainValidationException($"O motivo da recusa deve ter no máximo {RejectionReasonMaxLength} caracteres.");
        }

        Status = MedicationEntryStatus.Recusada;
        RejectionReason = reason;
        ReviewedByUserId = reviewedByUserId;
        ReviewedAt = reviewedAtUtc;
    }

    /// <summary>Indica se a entrada compõe o estoque.</summary>
    public bool CountsTowardStock => Status == MedicationEntryStatus.Conferida;

    /// <summary>
    /// Indica se o lote já venceu na data de referência.
    /// </summary>
    /// <remarks>Validade não identificada não é tratada como vencida — é tratada como risco.</remarks>
    public bool IsExpiredOn(DateOnly reference) => ExpiryDate is { } validade && validade < reference;

    private void SetData(
        int quantity,
        string? lotNumber,
        DateOnly? expiryDate,
        bool expiryNotIdentified,
        MedicationOrigin origin,
        DateOnly receivedOn,
        string? serialNumber,
        string? notes,
        DateOnly today)
    {
        lotNumber = Normalize(lotNumber);
        serialNumber = Normalize(serialNumber);
        notes = Normalize(notes);

        ValidarQuantidade(quantity);

        if (!Enum.IsDefined(origin))
        {
            throw new DomainValidationException("A origem informada é inválida.");
        }

        if (receivedOn > today)
        {
            throw new DomainValidationException("A data de recebimento não pode estar no futuro.");
        }

        // Exigir uma das duas obriga a decisão consciente. Sem isso, deixar a validade em
        // branco viraria o caminho fácil, e validade desconhecida é o principal risco em
        // medicamento doado — precisa aparecer para quem confere, não sumir.
        if (expiryNotIdentified && expiryDate is not null)
        {
            throw new DomainValidationException(
                "Não é possível informar a validade e ao mesmo tempo marcá-la como não identificada.");
        }

        if (!expiryNotIdentified && expiryDate is null)
        {
            throw new DomainValidationException(
                "Informe a validade ou marque que ela não pôde ser identificada na embalagem.");
        }

        if (lotNumber is { Length: > LotNumberMaxLength })
        {
            throw new DomainValidationException($"O lote deve ter no máximo {LotNumberMaxLength} caracteres.");
        }

        if (serialNumber is { Length: > SerialNumberMaxLength })
        {
            throw new DomainValidationException($"O número de série deve ter no máximo {SerialNumberMaxLength} caracteres.");
        }

        if (notes is { Length: > NotesMaxLength })
        {
            throw new DomainValidationException($"As observações devem ter no máximo {NotesMaxLength} caracteres.");
        }

        Quantity = quantity;
        LotNumber = lotNumber;
        ExpiryDate = expiryDate;
        ExpiryNotIdentified = expiryNotIdentified;
        Origin = origin;
        ReceivedOn = receivedOn;
        SerialNumber = serialNumber;
        Notes = notes;
    }

    private static void ValidarQuantidade(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainValidationException("A quantidade recebida deve ser maior que zero.");
        }

        if (quantity > MaxQuantity)
        {
            throw new DomainValidationException($"A quantidade recebida deve ser no máximo {MaxQuantity}.");
        }
    }

    private void GarantirPendente(string mensagem)
    {
        if (Status != MedicationEntryStatus.AguardandoConferencia)
        {
            throw new DomainValidationException(mensagem);
        }
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
