using LarSaoVicente.Domain.Entities;
using LarSaoVicente.Domain.Enums;
using LarSaoVicente.Tests.Infraestrutura;
using Microsoft.EntityFrameworkCore;

namespace LarSaoVicente.Tests.Integracao;

/// <summary>
/// Testes das restrições do banco para o catálogo (US09).
/// </summary>
/// <remarks>
/// Estas regras não existem no código C#: são do banco. Só um teste que grave de verdade
/// consegue verificá-las — foi o tipo de descuido que produziu quatro dos sete defeitos
/// da Sprint 1.
/// </remarks>
public class CatalogoPersistenciaTests : IAsyncLifetime
{
    private readonly AplicacaoDeTeste _aplicacao = new();

    public Task InitializeAsync() => ((IAsyncLifetime)_aplicacao).InitializeAsync();

    public Task DisposeAsync() => ((IAsyncLifetime)_aplicacao).DisposeAsync();

    private static Medication CriarMedicamento(string nome, string? codigoDeBarras = null)
        => new(nome, "Princípio de teste", "100 mg", PharmaceuticalForm.Comprimido,
            30, PackageUnit.Caixa, codigoDeBarras, MedicationCategory.Medicamento, null);

    [Fact]
    public async Task IndiceUnico_AceitaVariosItensSemCodigoDeBarras()
    {
        // A maior parte das cartelas de doação não tem código algum. Sem o filtro no
        // índice, o SQL Server trataria todos os NULL como iguais e aceitaria só um.
        await _aplicacao.ExecutarNoBancoAsync(async contexto =>
        {
            contexto.Medications.AddRange(
                CriarMedicamento("Sem código A"),
                CriarMedicamento("Sem código B"),
                CriarMedicamento("Sem código C"));

            await contexto.SaveChangesAsync();
        });

        var quantidade = await _aplicacao.ConsultarBancoAsync(
            c => c.Medications.CountAsync(m => m.Barcode == null));

        Assert.Equal(3, quantidade);
    }

    [Fact]
    public async Task IndiceUnico_RecusaDoisItensComOMesmoCodigo()
    {
        await _aplicacao.ExecutarNoBancoAsync(async contexto =>
        {
            contexto.Medications.Add(CriarMedicamento("Primeiro", "7891000315507"));
            await contexto.SaveChangesAsync();
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => _aplicacao.ExecutarNoBancoAsync(
            async contexto =>
            {
                contexto.Medications.Add(CriarMedicamento("Duplicado", "7891000315507"));
                await contexto.SaveChangesAsync();
            }));
    }

    [Fact]
    public async Task Gravar_PreencheOsCamposDeAuditoriaAutomaticamente()
    {
        Guid id = Guid.Empty;

        await _aplicacao.ExecutarNoBancoAsync(async contexto =>
        {
            var item = CriarMedicamento("Com auditoria");
            id = item.Id;
            contexto.Medications.Add(item);
            await contexto.SaveChangesAsync();
        });

        var gravado = await _aplicacao.ConsultarBancoAsync(
            c => c.Medications.SingleAsync(m => m.Id == id));

        Assert.NotEqual(default, gravado.CreatedAt);
        Assert.Null(gravado.UpdatedAt);
    }

    [Fact]
    public async Task Entrada_GravaVinculadaAoMedicamentoEVoltaComEle()
    {
        Guid medicamentoId = Guid.Empty;

        await _aplicacao.ExecutarNoBancoAsync(async contexto =>
        {
            var item = CriarMedicamento("Dipirona 500 mg");
            medicamentoId = item.Id;
            contexto.Medications.Add(item);

            // Onze envelopes em um registro só, como a enfermagem pediu.
            contexto.MedicationEntries.Add(new MedicationEntry(
                item.Id, 11, "L2026A", new DateOnly(2027, 10, 31), false,
                MedicationOrigin.Doacao, new DateOnly(2026, 9, 15), false, null, null,
                new DateOnly(2026, 9, 15)));

            await contexto.SaveChangesAsync();
        });

        var entrada = await _aplicacao.ConsultarBancoAsync(c => c.MedicationEntries
            .Include(e => e.Medication)
            .SingleAsync(e => e.MedicationId == medicamentoId));

        Assert.Equal(11, entrada.Quantity);
        Assert.Equal(MedicationEntryStatus.AguardandoConferencia, entrada.Status);
        Assert.False(entrada.CountsTowardStock);
        Assert.Equal("Dipirona 500 mg", entrada.Medication!.CommercialName);
    }

    [Fact]
    public async Task Entrada_NaoImpedeQueOMedicamentoSejaInativado()
    {
        // O catálogo é inativado, nunca excluído: o histórico de entradas continua válido.
        Guid medicamentoId = Guid.Empty;

        await _aplicacao.ExecutarNoBancoAsync(async contexto =>
        {
            var item = CriarMedicamento("Para inativar");
            medicamentoId = item.Id;
            contexto.Medications.Add(item);
            contexto.MedicationEntries.Add(new MedicationEntry(
                item.Id, 2, null, null, true, MedicationOrigin.Familia,
                new DateOnly(2026, 9, 15), false, null, null, new DateOnly(2026, 9, 15)));

            await contexto.SaveChangesAsync();
        });

        await _aplicacao.ExecutarNoBancoAsync(async contexto =>
        {
            var item = await contexto.Medications.SingleAsync(m => m.Id == medicamentoId);
            item.Deactivate();
            await contexto.SaveChangesAsync();
        });

        var gravado = await _aplicacao.ConsultarBancoAsync(
            c => c.Medications.SingleAsync(m => m.Id == medicamentoId));

        Assert.Equal(MedicationStatus.Inativo, gravado.Status);
        Assert.NotNull(gravado.UpdatedAt);
    }
}
