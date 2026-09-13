using LarSaoVicente.Domain.Common;
using LarSaoVicente.Domain.Enums;

namespace LarSaoVicente.Domain.Entities;

/// <summary>
/// Item do catálogo: um medicamento ou um insumo que a instituição controla (US09).
/// </summary>
/// <remarks>
/// Representa o <b>produto</b>, não a caixa que chegou nem a prescrição de alguém.
/// Lote, validade e origem pertencem à entrada (US10); quem toma o quê pertence ao
/// vínculo com o residente (US11). Reunir tudo em uma tabela só é o erro de modelagem
/// que inviabiliza a rastreabilidade — ver <c>docs/arquitetura.md</c>, seção 5.1.
/// <para>
/// A separação dos campos veio do dado real. Na planilha da instituição um item é uma
/// linha de texto única, no formato <c>AAS 100MG C/ 30CP</c>, que mistura nome,
/// concentração, quantidade por embalagem e forma. Assim não há como listar tudo de
/// 100 mg nem somar caixas de apresentações diferentes do mesmo princípio ativo.
/// </para>
/// </remarks>
public class Medication : AuditableEntity
{
    public const int CommercialNameMaxLength = 150;
    public const int ActiveIngredientMaxLength = 150;
    public const int StrengthMaxLength = 60;
    public const int NotesMaxLength = 1000;

    /// <summary>Maior quantidade por embalagem aceita, para barrar erro de digitação.</summary>
    public const int MaxUnitsPerPackage = 10_000;

    /// <summary>
    /// Identificador único do item.
    /// </summary>
    /// <remarks>
    /// <see cref="Guid"/> pelo mesmo motivo do cadastro de residentes: a URL não deve
    /// permitir descobrir o tamanho do catálogo nem percorrê-lo por tentativa e erro.
    /// </remarks>
    public Guid Id { get; private set; }

    /// <summary>Nome como aparece na caixa. Ex.: "Macrodantina".</summary>
    public string CommercialName { get; private set; } = string.Empty;

    /// <summary>
    /// Princípio ativo. Ex.: "Nitrofurantoína".
    /// </summary>
    /// <remarks>
    /// Obrigatório para medicamentos e sem sentido para insumos. É o campo que liga o
    /// mesmo remédio entre marcas e laboratórios: nas planilhas da instituição o mesmo
    /// item aparece ora pelo nome comercial, ora pelo princípio ativo, e nada relaciona
    /// os dois registros.
    /// </remarks>
    public string? ActiveIngredient { get; private set; }

    /// <summary>
    /// Concentração, como impressa na embalagem. Ex.: "100 mg", "0,004 mg/mL", "2 mg + 5 mg".
    /// </summary>
    /// <remarks>
    /// Texto, e não número com unidade: no catálogo real aparecem associações de dois
    /// fármacos, proporções por mililitro e unidades internacionais. Estruturar isso
    /// obrigaria a escolher entre recusar o cadastro e adivinhar — e adivinhar
    /// concentração é risco clínico. Opcional porque 28 dos 144 itens não a informam.
    /// </remarks>
    public string? Strength { get; private set; }

    /// <summary>Forma farmacêutica. Obrigatória para medicamentos.</summary>
    public PharmaceuticalForm? Form { get; private set; }

    /// <summary>Quantidade contida em uma embalagem. Ex.: 30, nos "C/ 30CP" da planilha.</summary>
    public int? UnitsPerPackage { get; private set; }

    /// <summary>Unidade em que o item é recebido e contado.</summary>
    public PackageUnit PackageUnit { get; private set; }

    /// <summary>
    /// Código de barras da embalagem, quando existir.
    /// </summary>
    /// <remarks>
    /// Opcional de propósito: cartela avulsa recebida por doação frequentemente não tem
    /// código algum, e o cadastro manual precisa continuar possível. O índice do banco é
    /// único, mas admite vários registros sem código.
    /// </remarks>
    public string? Barcode { get; private set; }

    public MedicationCategory Category { get; private set; }

    public MedicationStatus Status { get; private set; }

    public string? Notes { get; private set; }

    /// <summary>
    /// Forma comparável do nome e da concentração, usada na busca e no aviso de duplicata.
    /// </summary>
    /// <remarks>
    /// Derivada dos demais campos e mantida pela própria entidade, nunca atribuída de
    /// fora. Ver <see cref="SearchKey"/> para a razão de existir.
    /// </remarks>
    public string NormalizedSearchKey { get; private set; } = string.Empty;

    /// <summary>Construtor exigido pelo Entity Framework Core para materializar a entidade.</summary>
    private Medication()
    {
    }

    /// <summary>
    /// Cria um item de catálogo já validado, com situação <see cref="MedicationStatus.Ativo"/>.
    /// </summary>
    /// <exception cref="DomainValidationException">Quando alguma regra de negócio é violada.</exception>
    public Medication(
        string commercialName,
        string? activeIngredient,
        string? strength,
        PharmaceuticalForm? form,
        int? unitsPerPackage,
        PackageUnit packageUnit,
        string? barcode,
        MedicationCategory category,
        string? notes)
    {
        Id = Guid.NewGuid();
        Status = MedicationStatus.Ativo;
        SetData(commercialName, activeIngredient, strength, form, unitsPerPackage, packageUnit, barcode, category, notes);
    }

    /// <summary>
    /// Atualiza o item aplicando as mesmas validações do cadastro.
    /// </summary>
    /// <exception cref="DomainValidationException">Quando alguma regra de negócio é violada.</exception>
    public void Update(
        string commercialName,
        string? activeIngredient,
        string? strength,
        PharmaceuticalForm? form,
        int? unitsPerPackage,
        PackageUnit packageUnit,
        string? barcode,
        MedicationCategory category,
        string? notes)
    {
        SetData(commercialName, activeIngredient, strength, form, unitsPerPackage, packageUnit, barcode, category, notes);
    }

    /// <summary>Inativa o item, preservando as entradas e movimentações que apontam para ele.</summary>
    /// <remarks>Operação idempotente.</remarks>
    public void Deactivate() => Status = MedicationStatus.Inativo;

    /// <summary>Reativa um item anteriormente inativado.</summary>
    public void Reactivate() => Status = MedicationStatus.Ativo;

    /// <summary>
    /// Nome completo para exibição, reunindo o que a planilha guardava em um campo só.
    /// </summary>
    /// <remarks>
    /// Deriva dos campos separados em vez de ser armazenado: um texto gravado ficaria
    /// desatualizado assim que alguém corrigisse a concentração.
    /// </remarks>
    public string GetDisplayName()
    {
        var partes = new List<string> { CommercialName };

        if (!string.IsNullOrWhiteSpace(Strength))
        {
            partes.Add(Strength);
        }

        return string.Join(' ', partes);
    }

    private void SetData(
        string commercialName,
        string? activeIngredient,
        string? strength,
        PharmaceuticalForm? form,
        int? unitsPerPackage,
        PackageUnit packageUnit,
        string? barcode,
        MedicationCategory category,
        string? notes)
    {
        commercialName = Normalize(commercialName) ?? string.Empty;
        activeIngredient = Normalize(activeIngredient);
        strength = Normalize(strength);
        notes = Normalize(notes);
        barcode = Gtin.Normalize(barcode);

        if (!Enum.IsDefined(category))
        {
            throw new DomainValidationException("A categoria informada é inválida.");
        }

        if (!Enum.IsDefined(packageUnit))
        {
            throw new DomainValidationException("A unidade de embalagem informada é inválida.");
        }

        if (form is not null && !Enum.IsDefined(form.Value))
        {
            throw new DomainValidationException("A forma farmacêutica informada é inválida.");
        }

        if (commercialName.Length == 0)
        {
            throw new DomainValidationException("O nome do item é obrigatório.");
        }

        if (commercialName.Length > CommercialNameMaxLength)
        {
            throw new DomainValidationException($"O nome do item deve ter no máximo {CommercialNameMaxLength} caracteres.");
        }

        // Insumo não tem princípio ativo nem forma farmacêutica: luva e fralda não são
        // fármacos. Exigir esses campos obrigaria a equipe a inventar um valor.
        if (category == MedicationCategory.Medicamento)
        {
            if (string.IsNullOrEmpty(activeIngredient))
            {
                throw new DomainValidationException("O princípio ativo é obrigatório para medicamentos.");
            }

            if (form is null)
            {
                throw new DomainValidationException("A forma farmacêutica é obrigatória para medicamentos.");
            }
        }
        else
        {
            activeIngredient = null;
            form = null;
        }

        if (activeIngredient is { Length: > ActiveIngredientMaxLength })
        {
            throw new DomainValidationException($"O princípio ativo deve ter no máximo {ActiveIngredientMaxLength} caracteres.");
        }

        if (strength is { Length: > StrengthMaxLength })
        {
            throw new DomainValidationException($"A concentração deve ter no máximo {StrengthMaxLength} caracteres.");
        }

        if (unitsPerPackage is <= 0)
        {
            throw new DomainValidationException("A quantidade por embalagem deve ser maior que zero.");
        }

        if (unitsPerPackage > MaxUnitsPerPackage)
        {
            throw new DomainValidationException($"A quantidade por embalagem deve ser no máximo {MaxUnitsPerPackage}.");
        }

        if (barcode is not null && !Gtin.IsValid(barcode))
        {
            throw new DomainValidationException(
                "O código de barras informado é inválido. Confira os dígitos ou refaça a leitura.");
        }

        if (notes is { Length: > NotesMaxLength })
        {
            throw new DomainValidationException($"As observações devem ter no máximo {NotesMaxLength} caracteres.");
        }

        CommercialName = commercialName;
        ActiveIngredient = activeIngredient;
        Strength = strength;
        Form = form;
        UnitsPerPackage = unitsPerPackage;
        PackageUnit = packageUnit;
        Barcode = barcode;
        Category = category;
        Notes = notes;
        NormalizedSearchKey = SearchKey.From(commercialName, strength);
    }

    /// <summary>Remove espaços nas extremidades e converte texto vazio em <c>null</c>.</summary>
    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
