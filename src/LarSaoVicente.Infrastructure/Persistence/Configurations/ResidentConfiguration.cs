using LarSaoVicente.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LarSaoVicente.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento da entidade <see cref="Resident"/> para o banco de dados.
/// </summary>
/// <remarks>
/// O mapeamento fica separado da entidade para que a camada de domínio permaneça
/// livre de dependências do Entity Framework.
/// </remarks>
public class ResidentConfiguration : IEntityTypeConfiguration<Resident>
{
    public void Configure(EntityTypeBuilder<Resident> builder)
    {
        builder.ToTable("Residentes");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .ValueGeneratedNever();

        builder.Property(r => r.FullName)
            .IsRequired()
            .HasMaxLength(Resident.FullNameMaxLength);

        builder.Property(r => r.BirthDate)
            .IsRequired();

        builder.Property(r => r.AdmissionDate)
            .IsRequired();

        builder.Property(r => r.Room)
            .IsRequired()
            .HasMaxLength(Resident.RoomMaxLength);

        // Enums são gravados como inteiro: ocupam menos espaço e permitem renomear
        // o membro em C# sem migração de dados.
        builder.Property(r => r.DependencyLevel)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(r => r.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(r => r.Notes)
            .HasMaxLength(Resident.NotesMaxLength);

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        builder.Property(r => r.CreatedByUserId)
            .HasMaxLength(450);

        builder.Property(r => r.UpdatedByUserId)
            .HasMaxLength(450);

        // A listagem filtra por situação e ordena por nome (US03).
        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => r.FullName);
    }
}
