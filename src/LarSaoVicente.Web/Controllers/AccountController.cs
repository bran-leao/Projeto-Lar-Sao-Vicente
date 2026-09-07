using LarSaoVicente.Infrastructure.Identity;
using LarSaoVicente.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LarSaoVicente.Web.Controllers;

/// <summary>
/// Autenticação de funcionários (US01).
/// </summary>
/// <remarks>
/// Não existe autocadastro: as contas são criadas pela administração da instituição.
/// Por isso este controller expõe apenas login, logout e a página de acesso negado.
/// </remarks>
[Route("Conta")]
public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<AccountController> _logger;

    /// <summary>
    /// Mensagem única para credenciais incorretas.
    /// </summary>
    /// <remarks>
    /// Propositalmente não distingue "e-mail inexistente" de "senha incorreta": informar
    /// a diferença permitiria descobrir quais e-mails possuem conta no sistema.
    /// </remarks>
    private const string MensagemCredenciaisInvalidas = "E-mail ou senha inválidos.";

    public AccountController(SignInManager<ApplicationUser> signInManager, ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _logger = logger;
    }

    [HttpGet("Login")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        // Quem já está autenticado não precisa ver a tela de login novamente.
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost("Login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // lockoutOnFailure ativa o bloqueio temporário após tentativas seguidas de erro.
        var resultado = await _signInManager.PasswordSignInAsync(
            model.Email,
            model.Password,
            isPersistent: false,
            lockoutOnFailure: true);

        if (resultado.Succeeded)
        {
            _logger.LogInformation("Login realizado por {Email}.", model.Email);
            return RedirecionarAposLogin(returnUrl);
        }

        if (resultado.IsLockedOut)
        {
            _logger.LogWarning("Conta {Email} temporariamente bloqueada por excesso de tentativas.", model.Email);
            ModelState.AddModelError(
                string.Empty,
                "Conta temporariamente bloqueada por excesso de tentativas. Tente novamente em alguns minutos.");
            return View(model);
        }

        _logger.LogWarning("Tentativa de login malsucedida para {Email}.", model.Email);
        ModelState.AddModelError(string.Empty, MensagemCredenciaisInvalidas);
        return View(model);
    }

    /// <summary>Encerra a sessão do usuário.</summary>
    /// <remarks>
    /// Exposto apenas por POST. Fosse um link comum, bastaria um site externo carregar
    /// essa URL em uma imagem para desconectar o funcionário sem que ele solicitasse.
    /// </remarks>
    [HttpPost("Sair")]
    public async Task<IActionResult> Sair()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [HttpGet("AcessoNegado")]
    public IActionResult AcessoNegado() => View();

    /// <summary>
    /// Retorna o usuário à página que ele tentou acessar antes do login.
    /// </summary>
    /// <remarks>
    /// O endereço só é aceito quando aponta para dentro da própria aplicação. Sem essa
    /// verificação, um link manipulado poderia levar o funcionário a um site externo
    /// logo após a autenticação.
    /// </remarks>
    private IActionResult RedirecionarAposLogin(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Dashboard");
    }
}
