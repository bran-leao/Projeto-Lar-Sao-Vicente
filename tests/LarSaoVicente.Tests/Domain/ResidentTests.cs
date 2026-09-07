using LarSaoVicente.Domain.Common;
using LarSaoVicente.Domain.Entities;
using LarSaoVicente.Domain.Enums;

namespace LarSaoVicente.Tests.Domain;

/// <summary>
/// Testes das regras de negócio da entidade <see cref="Resident"/> (US02, US05 e US06).
/// </summary>
public class ResidentTests
{
    /// <summary>Data fixa usada como "hoje", para que os testes não dependam do relógio.</summary>
    private static readonly DateOnly Hoje = new(2026, 9, 7);

    private static Resident CriarResidenteValido(
        string nome = "Maria Oliveira",
        DateOnly? nascimento = null,
        DateOnly? entrada = null,
        string quarto = "12A",
        DependencyLevel grau = DependencyLevel.GrauII,
        string? observacoes = null)
    {
        return new Resident(
            nome,
            nascimento ?? new DateOnly(1945, 3, 20),
            entrada ?? new DateOnly(2020, 6, 1),
            quarto,
            grau,
            observacoes,
            Hoje);
    }

    [Fact]
    public void Criar_ComDadosValidos_PreencheCamposEIniciaAtivo()
    {
        var residente = CriarResidenteValido();

        Assert.NotEqual(Guid.Empty, residente.Id);
        Assert.Equal("Maria Oliveira", residente.FullName);
        Assert.Equal(new DateOnly(1945, 3, 20), residente.BirthDate);
        Assert.Equal(new DateOnly(2020, 6, 1), residente.AdmissionDate);
        Assert.Equal("12A", residente.Room);
        Assert.Equal(DependencyLevel.GrauII, residente.DependencyLevel);
        Assert.Equal(ResidentStatus.Ativo, residente.Status);
    }

    [Fact]
    public void Criar_ComDataDeNascimentoNoFuturo_Rejeita()
    {
        var excecao = Assert.Throws<DomainValidationException>(
            () => CriarResidenteValido(nascimento: Hoje.AddDays(1), entrada: Hoje));

        Assert.Contains("nascimento", excecao.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Criar_ComDataDeEntradaNoFuturo_Rejeita()
    {
        var excecao = Assert.Throws<DomainValidationException>(
            () => CriarResidenteValido(entrada: Hoje.AddDays(1)));

        Assert.Contains("entrada", excecao.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Criar_ComDatasDeHoje_Aceita()
    {
        // O limite é "não pode estar no futuro": a data de hoje continua válida.
        var residente = CriarResidenteValido(nascimento: Hoje, entrada: Hoje);

        Assert.Equal(Hoje, residente.BirthDate);
        Assert.Equal(Hoje, residente.AdmissionDate);
    }

    [Fact]
    public void Criar_ComEntradaAnteriorAoNascimento_Rejeita()
    {
        Assert.Throws<DomainValidationException>(() => CriarResidenteValido(
            nascimento: new DateOnly(1945, 3, 20),
            entrada: new DateOnly(1940, 1, 1)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Criar_SemNome_Rejeita(string nome)
    {
        Assert.Throws<DomainValidationException>(() => CriarResidenteValido(nome: nome));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Criar_SemQuarto_Rejeita(string quarto)
    {
        Assert.Throws<DomainValidationException>(() => CriarResidenteValido(quarto: quarto));
    }

    [Fact]
    public void Criar_ComNomeAcimaDoLimite_Rejeita()
    {
        var nomeLongo = new string('A', Resident.FullNameMaxLength + 1);

        Assert.Throws<DomainValidationException>(() => CriarResidenteValido(nome: nomeLongo));
    }

    [Fact]
    public void Criar_RemoveEspacosNasExtremidades()
    {
        var residente = CriarResidenteValido(nome: "  Antônio Souza  ", quarto: " 08 ");

        Assert.Equal("Antônio Souza", residente.FullName);
        Assert.Equal("08", residente.Room);
    }

    [Fact]
    public void Editar_ComDadosValidos_AtualizaMantendoIdentidadeESituacao()
    {
        var residente = CriarResidenteValido();
        var idOriginal = residente.Id;

        residente.Update(
            "José Ferreira",
            new DateOnly(1950, 1, 10),
            new DateOnly(2021, 2, 15),
            "07B",
            DependencyLevel.GrauIII,
            "Utiliza cadeira de rodas.",
            Hoje);

        Assert.Equal(idOriginal, residente.Id);
        Assert.Equal("José Ferreira", residente.FullName);
        Assert.Equal("07B", residente.Room);
        Assert.Equal(DependencyLevel.GrauIII, residente.DependencyLevel);
        Assert.Equal("Utiliza cadeira de rodas.", residente.Notes);
        Assert.Equal(ResidentStatus.Ativo, residente.Status);
    }

    [Fact]
    public void Editar_ComDataNoFuturo_RejeitaEPreservaDadosAnteriores()
    {
        var residente = CriarResidenteValido();

        Assert.Throws<DomainValidationException>(() => residente.Update(
            "José Ferreira",
            Hoje.AddDays(1),
            new DateOnly(2021, 2, 15),
            "07B",
            DependencyLevel.GrauIII,
            null,
            Hoje));

        // A entidade não pode ficar em estado parcialmente alterado após uma rejeição.
        Assert.Equal("Maria Oliveira", residente.FullName);
        Assert.Equal("12A", residente.Room);
        Assert.Equal(DependencyLevel.GrauII, residente.DependencyLevel);
    }

    [Fact]
    public void Inativar_AlteraSituacaoEPreservaOsDados()
    {
        var residente = CriarResidenteValido();

        residente.Deactivate();

        Assert.Equal(ResidentStatus.Inativo, residente.Status);
        Assert.Equal("Maria Oliveira", residente.FullName);
        Assert.Equal(new DateOnly(2020, 6, 1), residente.AdmissionDate);
    }

    [Fact]
    public void Inativar_DuasVezes_NaoProduzEfeitoAdicional()
    {
        var residente = CriarResidenteValido();

        residente.Deactivate();
        residente.Deactivate();

        Assert.Equal(ResidentStatus.Inativo, residente.Status);
    }

    [Fact]
    public void Reativar_VoltaSituacaoParaAtivo()
    {
        var residente = CriarResidenteValido();
        residente.Deactivate();

        residente.Reactivate();

        Assert.Equal(ResidentStatus.Ativo, residente.Status);
    }

    [Theory]
    // Aniversário já ocorrido no ano de referência.
    [InlineData("1945-03-20", "2026-09-07", 81)]
    // Aniversário ainda não ocorrido no ano de referência.
    [InlineData("1945-12-20", "2026-09-07", 80)]
    // Exatamente no dia do aniversário.
    [InlineData("1945-09-07", "2026-09-07", 81)]
    public void CalcularIdade_RetornaAnosCompletos(string nascimento, string referencia, int idadeEsperada)
    {
        var residente = CriarResidenteValido(nascimento: DateOnly.Parse(nascimento));

        var idade = residente.GetAgeOn(DateOnly.Parse(referencia));

        Assert.Equal(idadeEsperada, idade);
    }
}
