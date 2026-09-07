using System.Text.RegularExpressions;
using LarSaoVicente.Domain.Entities;
using LarSaoVicente.Domain.Enums;
using LarSaoVicente.Tests.Infraestrutura;

namespace LarSaoVicente.Tests.Integracao;

/// <summary>
/// Testes de integração do Dashboard (US07).
/// </summary>
/// <remarks>
/// Cada teste cria a própria instância da aplicação, com um banco vazio. As contagens
/// dependem do conteúdo exato do banco, então compartilhar a instância faria o resultado
/// variar conforme a ordem de execução dos testes.
/// </remarks>
public class DashboardTests
{
    private static async Task<AplicacaoDeTeste> CriarAplicacaoAsync()
    {
        var aplicacao = new AplicacaoDeTeste();
        await aplicacao.InitializeAsync();
        return aplicacao;
    }

    /// <summary>Lê os números exibidos nos indicadores da tela.</summary>
    private static List<int> ValoresExibidos(string html) =>
        Regex.Matches(html, """indicador__valor">(\d+)<""")
            .Select(m => int.Parse(m.Groups[1].Value))
            .ToList();

    [Fact]
    public async Task Dashboard_SemResidentes_ExibeZerosEOEstadoVazio()
    {
        await using var aplicacao = await CriarAplicacaoAsync();
        var cliente = aplicacao.CriarCliente();
        await cliente.AutenticarAsync();

        var html = await cliente.GetStringAsync("/");

        Assert.Contains("Ainda não há residentes cadastrados", html);
        Assert.Contains("Nenhum residente ativo no momento.", html);

        // Total, ativos e inativos: os três indicadores começam zerados.
        Assert.Equal([0, 0, 0], ValoresExibidos(html));
    }

    [Fact]
    public async Task Dashboard_ApresentaContagensReaisPorSituacaoEGrau()
    {
        await using var aplicacao = await CriarAplicacaoAsync();
        var cliente = aplicacao.CriarCliente();
        await cliente.AutenticarAsync();

        var hoje = new DateOnly(2026, 1, 10);
        var inativo = new Resident("Contagem Inativo", new DateOnly(1940, 1, 1),
            new DateOnly(2020, 1, 1), "C1", DependencyLevel.GrauI, null, hoje);
        inativo.Deactivate();

        await aplicacao.ConsultarBancoAsync(async contexto =>
        {
            contexto.Residents.AddRange(
                new Resident("Contagem Ativo I", new DateOnly(1940, 1, 1),
                    new DateOnly(2020, 1, 1), "C2", DependencyLevel.GrauI, null, hoje),
                new Resident("Contagem Ativo II A", new DateOnly(1941, 1, 1),
                    new DateOnly(2020, 1, 1), "C3", DependencyLevel.GrauII, null, hoje),
                new Resident("Contagem Ativo II B", new DateOnly(1942, 1, 1),
                    new DateOnly(2020, 1, 1), "C4", DependencyLevel.GrauII, null, hoje),
                inativo);

            return await contexto.SaveChangesAsync();
        });

        var valores = ValoresExibidos(await cliente.GetStringAsync("/"));

        // Ordem na tela: total, ativos, inativos, Grau I, Grau II, Grau III.
        // O residente inativo não entra na contagem por grau de dependência.
        Assert.Equal([4, 3, 1, 1, 2, 0], valores);
    }
}
