using LarSaoVicente.Domain.Common;
using LarSaoVicente.Domain.Entities;
using LarSaoVicente.Domain.Enums;

namespace LarSaoVicente.Tests.Domain;

/// <summary>
/// Testes das regras de negócio do catálogo (US09).
/// </summary>
/// <remarks>
/// Os exemplos reproduzem o formato encontrado nas planilhas da instituição, mas usam
/// apenas nomes de medicamentos — nenhum dado de residente é utilizado.
/// </remarks>
public class MedicationTests
{
    private const string Ean13Valido = "7891000315507";

    private static Medication CriarMedicamentoValido(
        string nome = "Macrodantina",
        string? principioAtivo = "Nitrofurantoína",
        string? concentracao = "100 mg",
        PharmaceuticalForm? forma = PharmaceuticalForm.Comprimido,
        int? porEmbalagem = 28,
        PackageUnit unidade = PackageUnit.Caixa,
        string? codigoBarras = null,
        MedicationCategory categoria = MedicationCategory.Medicamento,
        string? observacoes = null)
    {
        return new Medication(
            nome, principioAtivo, concentracao, forma, porEmbalagem,
            unidade, codigoBarras, categoria, observacoes);
    }

    [Fact]
    public void Criar_ComDadosValidos_PreencheCamposEIniciaAtivo()
    {
        var item = CriarMedicamentoValido();

        Assert.NotEqual(Guid.Empty, item.Id);
        Assert.Equal("Macrodantina", item.CommercialName);
        Assert.Equal("Nitrofurantoína", item.ActiveIngredient);
        Assert.Equal("100 mg", item.Strength);
        Assert.Equal(PharmaceuticalForm.Comprimido, item.Form);
        Assert.Equal(28, item.UnitsPerPackage);
        Assert.Equal(PackageUnit.Caixa, item.PackageUnit);
        Assert.Equal(MedicationCategory.Medicamento, item.Category);
        Assert.Equal(MedicationStatus.Ativo, item.Status);
        Assert.Null(item.Barcode);
    }

    [Fact]
    public void Criar_SemNome_Rejeita()
    {
        var excecao = Assert.Throws<DomainValidationException>(() => CriarMedicamentoValido(nome: "   "));

        Assert.Contains("nome", excecao.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Criar_ComNomeAcimaDoLimite_Rejeita()
    {
        var excecao = Assert.Throws<DomainValidationException>(
            () => CriarMedicamentoValido(nome: new string('A', Medication.CommercialNameMaxLength + 1)));

        Assert.Contains("nome", excecao.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Criar_MedicamentoSemPrincipioAtivo_Rejeita()
    {
        // É o campo que liga o mesmo remédio entre marcas diferentes.
        var excecao = Assert.Throws<DomainValidationException>(
            () => CriarMedicamentoValido(principioAtivo: null));

        Assert.Contains("princípio ativo", excecao.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Criar_MedicamentoSemFormaFarmaceutica_Rejeita()
    {
        var excecao = Assert.Throws<DomainValidationException>(() => CriarMedicamentoValido(forma: null));

        Assert.Contains("forma", excecao.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Criar_InsumoSemPrincipioAtivoNemForma_Aceita()
    {
        // Luva e fralda não são fármacos: exigir esses campos obrigaria a inventar valor.
        var item = CriarMedicamentoValido(
            nome: "Luva de procedimento M",
            principioAtivo: null,
            concentracao: null,
            forma: null,
            porEmbalagem: 100,
            unidade: PackageUnit.Caixa,
            categoria: MedicationCategory.Insumo);

        Assert.Equal(MedicationCategory.Insumo, item.Category);
        Assert.Null(item.ActiveIngredient);
        Assert.Null(item.Form);
    }

    [Fact]
    public void Criar_InsumoComPrincipioAtivo_IgnoraOCampo()
    {
        var item = CriarMedicamentoValido(
            nome: "Fralda geriátrica G",
            principioAtivo: "não se aplica",
            forma: PharmaceuticalForm.Comprimido,
            categoria: MedicationCategory.Insumo);

        Assert.Null(item.ActiveIngredient);
        Assert.Null(item.Form);
    }

    [Fact]
    public void Criar_SemConcentracao_Aceita()
    {
        // 28 dos 144 itens do catálogo real não informam concentração.
        var item = CriarMedicamentoValido(nome: "Complexo B", principioAtivo: "Vitaminas do complexo B", concentracao: null);

        Assert.Null(item.Strength);
    }

    [Fact]
    public void Criar_SemQuantidadePorEmbalagem_Aceita()
    {
        var item = CriarMedicamentoValido(
            nome: "Systane",
            principioAtivo: "Polietilenoglicol",
            concentracao: null,
            forma: PharmaceuticalForm.Colirio,
            porEmbalagem: null,
            unidade: PackageUnit.Frasco);

        Assert.Null(item.UnitsPerPackage);
        Assert.Equal(PackageUnit.Frasco, item.PackageUnit);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Criar_ComQuantidadePorEmbalagemNaoPositiva_Rejeita(int quantidade)
    {
        var excecao = Assert.Throws<DomainValidationException>(
            () => CriarMedicamentoValido(porEmbalagem: quantidade));

        Assert.Contains("quantidade", excecao.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Criar_ComQuantidadePorEmbalagemAcimaDoLimite_Rejeita()
    {
        var excecao = Assert.Throws<DomainValidationException>(
            () => CriarMedicamentoValido(porEmbalagem: Medication.MaxUnitsPerPackage + 1));

        Assert.Contains("quantidade", excecao.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Criar_ComCodigoDeBarrasValido_Aceita()
    {
        var item = CriarMedicamentoValido(codigoBarras: Ean13Valido);

        Assert.Equal(Ean13Valido, item.Barcode);
    }

    [Fact]
    public void Criar_ComCodigoDeBarrasSeparadoPorEspacos_Normaliza()
    {
        var item = CriarMedicamentoValido(codigoBarras: " 789 1000 3155 07 ");

        Assert.Equal(Ean13Valido, item.Barcode);
    }

    [Fact]
    public void Criar_ComCodigoDeBarrasInvalido_Rejeita()
    {
        var excecao = Assert.Throws<DomainValidationException>(
            () => CriarMedicamentoValido(codigoBarras: "7891000315508"));

        Assert.Contains("código de barras", excecao.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Criar_SemCodigoDeBarras_Aceita()
    {
        // Cartela avulsa de doação costuma não ter código algum.
        Assert.Null(CriarMedicamentoValido(codigoBarras: "  ").Barcode);
    }

    [Fact]
    public void Criar_ComCategoriaInvalida_Rejeita()
    {
        var excecao = Assert.Throws<DomainValidationException>(
            () => CriarMedicamentoValido(categoria: (MedicationCategory)99));

        Assert.Contains("categoria", excecao.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Criar_ComUnidadeDeEmbalagemInvalida_Rejeita()
    {
        var excecao = Assert.Throws<DomainValidationException>(
            () => CriarMedicamentoValido(unidade: (PackageUnit)42));

        Assert.Contains("unidade", excecao.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Criar_ComObservacoesAcimaDoLimite_Rejeita()
    {
        var excecao = Assert.Throws<DomainValidationException>(
            () => CriarMedicamentoValido(observacoes: new string('x', Medication.NotesMaxLength + 1)));

        Assert.Contains("observações", excecao.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ChaveDeBusca_IgnoraEspacoEntreNumeroEUnidade()
    {
        // Achado real: catorze itens da planilha estão escritos nas duas formas.
        var comEspaco = CriarMedicamentoValido(nome: "Losartana", principioAtivo: "Losartana potássica", concentracao: "50 mg");
        var semEspaco = CriarMedicamentoValido(nome: "Losartana", principioAtivo: "Losartana potássica", concentracao: "50mg");

        Assert.Equal(comEspaco.NormalizedSearchKey, semEspaco.NormalizedSearchKey);
    }

    [Fact]
    public void ChaveDeBusca_IgnoraAcentoMaiusculaEPontuacao()
    {
        var umaGrafia = CriarMedicamentoValido(nome: "Vitamina D", principioAtivo: "Colecalciferol", concentracao: "7.000 UI");
        var outraGrafia = CriarMedicamentoValido(nome: "VITAMINA D", principioAtivo: "Colecalciferol", concentracao: "7000 ui");

        Assert.Equal(umaGrafia.NormalizedSearchKey, outraGrafia.NormalizedSearchKey);
    }

    [Fact]
    public void ChaveDeBusca_DistingueConcentracoesDiferentes()
    {
        // Sinvastatina 20 mg e 40 mg são itens distintos e precisam continuar distintos.
        var vinte = CriarMedicamentoValido(nome: "Sinvastatina", principioAtivo: "Sinvastatina", concentracao: "20 mg");
        var quarenta = CriarMedicamentoValido(nome: "Sinvastatina", principioAtivo: "Sinvastatina", concentracao: "40 mg");

        Assert.NotEqual(vinte.NormalizedSearchKey, quarenta.NormalizedSearchKey);
    }

    [Fact]
    public void GetDisplayName_ReuneNomeEConcentracao()
    {
        Assert.Equal("Macrodantina 100 mg", CriarMedicamentoValido().GetDisplayName());
    }

    [Fact]
    public void GetDisplayName_SemConcentracao_UsaApenasONome()
    {
        var item = CriarMedicamentoValido(nome: "Complexo B", principioAtivo: "Vitaminas do complexo B", concentracao: null);

        Assert.Equal("Complexo B", item.GetDisplayName());
    }

    [Fact]
    public void Atualizar_ComDadosValidos_AlteraOsCampos()
    {
        var item = CriarMedicamentoValido();

        item.Update(
            "Nitrofurantoína", "Nitrofurantoína", "100 mg", PharmaceuticalForm.Capsula,
            30, PackageUnit.Caixa, Ean13Valido, MedicationCategory.Medicamento, "Antibiótico urinário.");

        Assert.Equal("Nitrofurantoína", item.CommercialName);
        Assert.Equal(PharmaceuticalForm.Capsula, item.Form);
        Assert.Equal(30, item.UnitsPerPackage);
        Assert.Equal(Ean13Valido, item.Barcode);
        Assert.Equal("Antibiótico urinário.", item.Notes);
    }

    [Fact]
    public void Atualizar_AplicaAsMesmasValidacoesDoCadastro()
    {
        var item = CriarMedicamentoValido();

        Assert.Throws<DomainValidationException>(() => item.Update(
            "", "Nitrofurantoína", "100 mg", PharmaceuticalForm.Comprimido,
            28, PackageUnit.Caixa, null, MedicationCategory.Medicamento, null));

        // O item permanece íntegro após a tentativa recusada.
        Assert.Equal("Macrodantina", item.CommercialName);
    }

    [Fact]
    public void Atualizar_RecalculaAChaveDeBusca()
    {
        var item = CriarMedicamentoValido();
        var chaveOriginal = item.NormalizedSearchKey;

        item.Update(
            "Macrodantina", "Nitrofurantoína", "50 mg", PharmaceuticalForm.Comprimido,
            28, PackageUnit.Caixa, null, MedicationCategory.Medicamento, null);

        Assert.NotEqual(chaveOriginal, item.NormalizedSearchKey);
    }

    [Fact]
    public void Inativar_MudaASituacaoEEIdempotente()
    {
        var item = CriarMedicamentoValido();

        item.Deactivate();
        item.Deactivate();

        Assert.Equal(MedicationStatus.Inativo, item.Status);
    }

    [Fact]
    public void Reativar_VoltaASituacaoParaAtivo()
    {
        var item = CriarMedicamentoValido();
        item.Deactivate();

        item.Reactivate();

        Assert.Equal(MedicationStatus.Ativo, item.Status);
    }
}
