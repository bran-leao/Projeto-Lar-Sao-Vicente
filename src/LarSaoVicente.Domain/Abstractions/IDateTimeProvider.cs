namespace LarSaoVicente.Domain.Abstractions;

/// <summary>
/// Fornece a data e a hora correntes.
/// </summary>
/// <remarks>
/// Existe como abstração por dois motivos:
/// 1. as regras de negócio dependem da data atual (por exemplo, "a data de nascimento
///    não pode estar no futuro") e precisam ser testáveis de forma determinística;
/// 2. a instituição é brasileira, então "hoje" deve ser sempre avaliado no fuso
///    America/Sao_Paulo, independentemente do fuso configurado no servidor.
/// </remarks>
public interface IDateTimeProvider
{
    /// <summary>Instante atual em UTC. Utilizado nos campos de auditoria.</summary>
    DateTime UtcNow { get; }

    /// <summary>Data de hoje no fuso horário de referência da instituição (America/Sao_Paulo).</summary>
    DateOnly Today { get; }
}
