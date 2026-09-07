namespace LarSaoVicente.Domain.Common;

/// <summary>
/// Base para entidades que precisam registrar quem as criou e alterou, e quando.
/// </summary>
/// <remarks>
/// Os valores são preenchidos automaticamente pelo <c>AppDbContext</c> durante o
/// <c>SaveChanges</c>, e não pelos controllers. Isso garante que nenhuma gravação
/// escape da auditoria e prepara o sistema para o requisito de rastreabilidade
/// levantado com a instituição.
/// <para>
/// As datas são gravadas em UTC. A conversão para o fuso de exibição
/// (America/Sao_Paulo) é responsabilidade da camada de apresentação.
/// </para>
/// </remarks>
public abstract class AuditableEntity
{
    /// <summary>Momento da criação do registro, em UTC.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Identificador do usuário que criou o registro.</summary>
    public string? CreatedByUserId { get; set; }

    /// <summary>Momento da última alteração, em UTC. Nulo enquanto o registro nunca foi alterado.</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Identificador do usuário que realizou a última alteração.</summary>
    public string? UpdatedByUserId { get; set; }
}
