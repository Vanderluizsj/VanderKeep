using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VKeeper.Aplicacao.Comum;
using VKeeper.Aplicacao.Notas;
using VKeeper.Aplicacao.Notas.Lixeira;
using VKeeper.Web.ViewModels.Trash;

namespace VKeeper.Web.Controllers;

[Authorize]
public sealed class TrashController : Controller
{
    private const int DefaultPageSize = 12;

    private readonly INoteService _noteService;
    private readonly ILogger<TrashController> _logger;

    public TrashController(
        INoteService noteService,
        ILogger<TrashController> logger)
    {
        _noteService = noteService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, CancellationToken cancellationToken = default)
    {
        var result = await _noteService.ListTrashAsync(
            new ListTrashQuery(Math.Max(1, page), DefaultPageSize),
            cancellationToken);

        if (result.IsFailure)
        {
            return MapFailure(result);
        }

        return View(new TrashListViewModel
        {
            Notes = result.Value
        });
    }

    [HttpPost]
    public async Task<IActionResult> Restore(Guid id, CancellationToken cancellationToken)
    {
        var result = await _noteService.RestoreAsync(id, cancellationToken);
        if (result.IsFailure)
        {
            return MapFailure(result);
        }

        _logger.LogInformation("Note restored from trash. NoteId: {NoteId}", id);
        TempData["SuccessMessage"] = "Nota restaurada para suas notas ativas.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _noteService.GetTrashItemByIdAsync(id, cancellationToken);
        if (result.IsFailure)
        {
            return MapFailure(result);
        }

        var note = result.Value;
        return View(new DeleteNotePermanentlyViewModel
        {
            Id = note.Id,
            Title = note.Title,
            Priority = note.Priority,
            DeletedAtUtc = note.DeletedAtUtc,
            Categories = note.Categories
        });
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(Guid id, CancellationToken cancellationToken)
    {
        var result = await _noteService.DeletePermanentlyAsync(id, cancellationToken);
        if (result.IsFailure)
        {
            return MapFailure(result);
        }

        _logger.LogInformation("Note permanently deleted from trash. NoteId: {NoteId}", id);
        TempData["SuccessMessage"] = "Nota excluída permanentemente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet, ActionName("Empty")]
    public async Task<IActionResult> ConfirmEmpty(CancellationToken cancellationToken)
    {
        var result = await _noteService.ListTrashAsync(new ListTrashQuery(1, 1), cancellationToken);
        if (result.IsFailure)
        {
            return MapFailure(result);
        }

        if (result.Value.TotalCount == 0)
        {
            TempData["InfoMessage"] = "Sua lixeira já está vazia.";
            return RedirectToAction(nameof(Index));
        }

        ViewData["TrashCount"] = result.Value.TotalCount;
        return View("Empty");
    }

    [HttpPost, ActionName("Empty")]
    public async Task<IActionResult> EmptyConfirmed(CancellationToken cancellationToken)
    {
        var result = await _noteService.EmptyTrashAsync(cancellationToken);
        if (result.IsFailure)
        {
            return MapFailure(result);
        }

        _logger.LogInformation("Trash emptied. RemovedCount: {RemovedCount}", result.Value);
        TempData["SuccessMessage"] = result.Value > 0
            ? $"Lixeira esvaziada com sucesso ({result.Value} {(result.Value == 1 ? "nota removida" : "notas removidas")})."
            : "Sua lixeira já está vazia.";
        return RedirectToAction(nameof(Index));
    }

    private IActionResult MapFailure(Result result)
    {
        return result.ErrorType switch
        {
            ErrorType.NotFound => NotFound(),
            ErrorType.Forbidden => Forbid(),
            _ => BadRequest()
        };
    }
}
