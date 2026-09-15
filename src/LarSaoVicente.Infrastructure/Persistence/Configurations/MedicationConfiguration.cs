using LarSaoVicente.Domain.Common;
using LarSaoVicente.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LarSaoVicente.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento da entidade <see cref="Medication"/> para o banco de dados.
/// </summary>
public class MedicationConfiguration : IEntityTypeConfiguration<Medication>
{
    public void Configure(EntityTypeBuilder<Medication> builder)
    {
        builder.ToTable("Medicamentos");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .ValueGeneratedNever();

        builder.Property(m => m.CommercialName)
            .IsRequired()
            .HasMaxLength(Medication.CommercialNameMaxLength);

        builder.Property(m => m.ActiveIngredient)
            .HasMaxLength(Medication.ActiveIngredientMaxLength);

        builder.Property(m => m.Strength)
            .HasMaxLength(Medication.StrengthMaxLength);

        builder.Property(m => m.Form)
            .HasConversion<int>();

        builder.Property(m => m.PackageUnit)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(m => m.Category)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(m => m.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(m => m.Barcode)
            .HasMaxLength(Gtin.MaxLength);

        builder.Property(m => m.NormalizedSearchKey)
            .IsRequired()
            .HasMaxLength(SearchKey.MaxLength);

        builder.Property(m => m.Notes)
            .HasMaxLength(Medication.NotesMaxLength);

        builder.Property(m => m.CreatedAt)
            .IsRequired();

        builder.Property(m => m.CreatedByUserId)
            .HasMaxLength(450);

        builder.Property(m => m.UpdatedByUserId)
            .HasMaxLength(450);

        // Índice único filtrado. No SQL Server um índice único comum trata todos os
        // NULL como iguais e aceitaria apenas um item sem código de barras — e a maior
        // parte das cartelas de doação não tem código algum. O filtro restringe a
        // unicidade às linhas que de fato possuem código.
        builder.HasIndex(m => m.Barcode)
            .IsUnique()
            .HasFilter("[Barcode] IS NOT NULL");

        // Busca por nome comercial ou princípio ativo (US24) e a detecção de duplicata.
        builder.HasIndex(m => m.NormalizedSearchKey);
        builder.HasIndex(m => m.ActiveIngredient);
        builder.HasIndex(m => m.Status);
    }
}
