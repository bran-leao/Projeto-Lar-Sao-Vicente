using LarSaoVicente.Domain.Abstractions;

namespace LarSaoVicente.Infrastructure.Services;

/// <summary>
/// Implementação padrão de <see cref="IDateTimeProvider"/>, baseada no relógio do sistema.
/// </summary>
/// <remarks>
/// A data corrente é sempre convertida para o fuso America/Sao_Paulo. Assim, o sistema
/// se comporta da mesma forma se um dia for hospedado em um servidor configurado em UTC
/// ou em outro país — situação em que "hoje" poderia divergir do dia real no Brasil.
/// </remarks>
public class SystemDateTimeProvider : IDateTimeProvider
{
    /// <summary>
    /// Identificador IANA do fuso de referência. O .NET aceita esse formato tanto no
    /// Windows quanto no Linux, evitando um identificador diferente por sistema operacional.
    /// </summary>
    private const string TimeZoneId = "America/Sao_Paulo";

    private static readonly TimeZoneInfo InstitutionTimeZone = ResolveTimeZone();

    public DateTime UtcNow => DateTime.UtcNow;

    public DateOnly Today => DateOnly.FromDateTime(ToInstitutionTime(DateTime.UtcNow));

    public DateTime ToInstitutionTime(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), InstitutionTimeZone);

    private static TimeZoneInfo ResolveTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            // Ambientes sem a base de fusos instalada (por exemplo, um contêiner mínimo)
            // recorrem ao horário de Brasília fixo, que não possui horário de verão desde 2019.
            return TimeZoneInfo.CreateCustomTimeZone(TimeZoneId, TimeSpan.FromHours(-3), TimeZoneId, TimeZoneId);
        }
    }
}
