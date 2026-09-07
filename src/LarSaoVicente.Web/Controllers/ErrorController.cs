using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace LarSaoVicente.Web.Controllers;

/// <summary>
/// Páginas de erro exibidas fora do ambiente de desenvolvimento.
/// </summary>
/// <remarks>
/// Mostra uma mensagem genérica ao usuário e registra o detalhe apenas no log.
/// Exibir a exceção na tela revelaria caminhos de arquivos e estrutura interna
/// do sistema a quem não deveria vê-los.
/// </remarks>
[AllowAnonymous]
[Route("Erro")]
public class ErrorController : Controller
{
    private readonly ILogger<ErrorController> _logger;

    public ErrorController(ILogger<ErrorController> logger)
    {
        _logger = logger;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        var detalhes = HttpContext.Features.Get<IExceptionHandlerPathFeature>();

        if (detalhes is not null)
        {
            _logger.LogError(detalhes.Error, "Erro não tratado em {Caminho}.", detalhes.Path);
        }

        ViewData["Title"] = "Erro";
        ViewData["Mensagem"] = "Ocorreu um erro inesperado ao processar a sua solicitação.";
        return View("Erro");
    }

    [HttpGet("{codigo:int}")]
    public IActionResult PorCodigo(int codigo)
    {
        ViewData["Title"] = codigo == StatusCodes.Status404NotFound ? "Página não encontrada" : "Erro";
        ViewData["Mensagem"] = codigo == StatusCodes.Status404NotFound
            ? "A página que você tentou acessar não existe."
            : "Não foi possível concluir a operação.";

        Response.StatusCode = codigo;
        return View("Erro");
    }
}
