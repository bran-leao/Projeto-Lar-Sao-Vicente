namespace LarSaoVicente.Domain.Common;

/// <summary>
/// Lançada quando uma regra de negócio do domínio é violada.
/// </summary>
/// <remarks>
/// Funciona como rede de segurança: a validação principal acontece nos ViewModels,
/// para que o usuário receba mensagens amigáveis no formulário. Esta exceção garante
/// que uma entidade inválida nunca chegue ao banco, mesmo que a validação da
/// interface seja contornada.
/// </remarks>
public class DomainValidationException : Exception
{
    public DomainValidationException(string message) : base(message)
    {
    }
}
