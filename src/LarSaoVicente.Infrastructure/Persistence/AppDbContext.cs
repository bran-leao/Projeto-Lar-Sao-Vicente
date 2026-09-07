using LarSaoVicente.Domain.Abstractions;
using LarSaoVicente.Domain.Common;
using LarSaoVicente.Domain.Entities;
using LarSaoVicente.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LarSaoVicente.Infrastructure.Persistence;

/// <summary>
/// Contexto de dados da aplicação.
/// </summary>
/// <remarks>
/// Herda de <see cref="IdentityDbContext{TUser}"/> para que usuários, perfis e senhas
/// fiquem no mesmo banco das demais informações, sob uma única transação.
/// </remarks>
public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUser _currentUser;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        IDateTimeProvider dateTimeProvider,
        ICurrentUser currentUser)
        : base(options)
    {
        _dateTimeProvider = dateTimeProvider;
        _currentUser = currentUser;
    }

    public DbSet<Resident> Residents => Set<Resident>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Carrega todas as classes IEntityTypeConfiguration deste assembly.
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override int SaveChanges()
    {
        ApplyAuditInformation();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInformation();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Preenche os campos de auditoria de todas as entidades alteradas.
    /// </summary>
    /// <remarks>
    /// Centralizar isso no contexto garante que nenhuma gravação escape da auditoria,
    /// mesmo que um controller futuro esqueça de preencher os campos manualmente.
    /// </remarks>
    private void ApplyAuditInformation()
    {
        var agora = _dateTimeProvider.UtcNow;
        var usuario = _currentUser.UserId;

        foreach (var entrada in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entrada.State)
            {
                case EntityState.Added:
                    entrada.Entity.CreatedAt = agora;
                    entrada.Entity.CreatedByUserId = usuario;
                    break;

                case EntityState.Modified:
                    entrada.Entity.UpdatedAt = agora;
                    entrada.Entity.UpdatedByUserId = usuario;

                    // Impede que a informação de criação seja sobrescrita em uma edição.
                    entrada.Property(e => e.CreatedAt).IsModified = false;
                    entrada.Property(e => e.CreatedByUserId).IsModified = false;
                    break;
            }
        }
    }
}
