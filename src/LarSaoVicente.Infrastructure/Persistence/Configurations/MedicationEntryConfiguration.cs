using LarSaoVicente.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LarSaoVicente.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento da entidade <see cref="MedicationEntry"/> para o banco de dados.
/// </summary>
public class MedicationEntryConfiguration : IEntityTypeConfiguration<MedicationEntry>
{
    public void Configure(EntityTypeBuilder<MedicationEntry> builder)
    {
        builder.ToTable("EntradasDeMedicamento");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.Quantity)
            .IsRequired();

        builder.Property(e => e.LotNumber)
            .HasMaxLength(MedicationEntry.LotNumberMaxLength);

        builder.Property(e => e.SerialNumber)
            .HasMaxLength(MedicationEntry.SerialNumberMaxLength);

        builder.Property(e => e.RejectionReason)
            .HasMaxLength(MedicationEntry.RejectionReasonMaxLength);

        builder.Property(e => e.Notes)
            .HasMaxLength(MedicationEntry.NotesMaxLength);

        builder.Property(e => e.Origin)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.ReceivedOn)
            .IsRequired();

        builder.Property(e => e.ReviewedByUserId)
            .HasMaxLength(450);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.CreatedByUserId)
            .HasMaxLength(450);

        builder.Property(e => e.UpdatedByUserId)
            .HasMaxLength(450);

        // Restrict, e não Cascade: apagar um item de catálogo não pode levar junto o
        // histórico de tudo que já entrou por ele. O catálogo é inativado, nunca excluído.
        builder.HasOne(e => e.Medication)
            .WithMany()
            .HasForeignKey(e => e.MedicationId)
            .OnDelete(DeleteBehavior.Restrict);

        // A tela de conferência lista o que está pendente (US27); o estoque soma o que
        // está conferido por medicamento (US13).
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => new { e.MedicationId, e.Status });
        builder.HasIndex(e => e.ExpiryDate);
    }
}
