using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VKeeper.Web.ViewModels;

namespace VKeeper.Web.Controllers;

[IgnoreAntiforgeryToken]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class ErrorController : Controller
{
    private readonly ILogger<ErrorController> _logger;

    public ErrorController(ILogger<ErrorController> logger)
    {
        _logger = logger;
    }

    [Route("/Error")]
    public IActionResult Index()
    {
        var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();

        if (exceptionFeature?.Error is not null)
        {
            _logger.LogError(
                exceptionFeature.Error,
                "Unhandled exception on path {OriginalPath}. RequestId: {RequestId}",
                exceptionFeature.Path,
                requestId);
        }
        else
        {
            _logger.LogWarning("Direct access to /Error without an active exception. RequestId: {RequestId}", requestId);
        }

        Response.StatusCode = StatusCodes.Status500InternalServerError;
        return View(new ErrorViewModel
        {
            RequestId = requestId,
            StatusCode = StatusCodes.Status500InternalServerError
        });
    }

    [Route("/Error/{statusCode:int}")]
    public IActionResult HttpStatusCodeHandler(int statusCode)
    {
        var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var normalizedStatusCode = statusCode is >= 400 and <= 599
            ? statusCode
            : StatusCodes.Status404NotFound;

        Response.StatusCode = normalizedStatusCode;

        return normalizedStatusCode switch
        {
            StatusCodes.Status404NotFound => View("NotFound", new ErrorViewModel
            {
                RequestId = requestId,
                StatusCode = StatusCodes.Status404NotFound,
                Title = "Página não encontrada",
                Message = "O recurso solicitado não existe ou foi movido."
            }),
            StatusCodes.Status403Forbidden => View("Forbidden", new ErrorViewModel
            {
                RequestId = requestId,
                StatusCode = StatusCodes.Status403Forbidden,
                Title = "Acesso negado",
                Message = "Você não tem permissão para acessar este recurso."
            }),
            _ => View("Index", new ErrorViewModel
            {
                RequestId = requestId,
                StatusCode = normalizedStatusCode,
                Title = $"Erro {normalizedStatusCode}",
                Message = "Não foi possível concluir a solicitação."
            })
        };
    }
}
