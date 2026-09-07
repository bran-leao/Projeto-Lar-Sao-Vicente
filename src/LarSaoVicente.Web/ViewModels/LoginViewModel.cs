using System.ComponentModel.DataAnnotations;

namespace LarSaoVicente.Web.ViewModels;

/// <summary>Dados informados na tela de login (US01).</summary>
public class LoginViewModel
{
    [Display(Name = "E-mail")]
    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Senha")]
    [Required(ErrorMessage = "Informe a senha.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}
