using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VKeeper.Aplicacao.Categorias;
using VKeeper.Aplicacao.Categorias.ListarCategorias;
using VKeeper.Aplicacao.Comum;
using VKeeper.Aplicacao.Notas;
using VKeeper.Aplicacao.Notas.AtualizarNota;
using VKeeper.Aplicacao.Notas.CriarNota;
using VKeeper.Aplicacao.Notas.ListarNotas;
using VKeeper.Dominio.Enums;
using VKeeper.Web.ViewModels.Notes;

namespace VKeeper.Web.Controllers;

[Authorize]
public sealed class NotesController : Controller
{
    private const int DefaultPageSize = 12;

    private readonly INoteService _noteService;
    private readonly ICategoryService _categoryService;
    private readonly ILogger<NotesController> _logger;

    public NotesController(
        INoteService noteService,
        ICategoryService categoryService,
        ILogger<NotesController> logger)
    {
        _noteService = noteService;
        _categoryService = categoryService;
        _logger = logger;
    }

    [HttpGet]
    public Task<IActionResult> Index(
        string? searchTerm,
        Guid? categoryId,
        NotePriority? priority,
        NoteSortBy sortBy = NoteSortBy.UpdatedAtDesc,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        return BuildNotesListViewAsync(
            isArchived: false,
            searchTerm,
            categoryId,
            priority,
            sortBy,
            page,
            cancellationToken);
    }

    [HttpGet]
    public Task<IActionResult> Archived(
        string? searchTerm,
        Guid? categoryId,
        NotePriority? priority,
        NoteSortBy sortBy = NoteSortBy.UpdatedAtDesc,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        return BuildNotesListViewAsync(
            isArchived: true,
            searchTerm,
            categoryId,
            priority,
            sortBy,
            page,
            cancellationToken);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var result = await _noteService.GetByIdAsync(id, cancellationToken);
        if (result.IsFailure)
        {
            return MapFailure(result);
        }

        var note = result.Value;
        return View(new NoteDetailsViewModel
        {
            Id = note.Id,
            Title = note.Title,
            Description = note.Description,
            Priority = note.Priority,
            IsPinned = note.IsPinned,
            IsArchived = note.IsArchived,
            CreatedAtUtc = note.CreatedAtUtc,
            UpdatedAtUtc = note.UpdatedAtUtc,
            Categories = note.Categories
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var categories = await LoadAvailableCategoriesAsync(cancellationToken);
        return View(new CreateNoteViewModel
        {
            Priority = NotePriority.Medium,
            AvailableCategories = categories
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateNoteViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableCategories = await LoadAvailableCategoriesAsync(cancellationToken);
            return View(model);
        }

        var result = await _noteService.CreateAsync(
            new CreateNoteRequest(model.Title, model.Description, model.Priority, model.SelectedCategoryIds),
            cancellationToken);

        if (result.IsFailure)
        {
            if (result.ErrorType is ErrorType.Validation or ErrorType.Conflict)
            {
                AddErrorsToModelState(result);
                model.AvailableCategories = await LoadAvailableCategoriesAsync(cancellationToken);
                return View(model);
            }

            return MapFailure(result);
        }

        _logger.LogInformation("Note created. NoteId: {NoteId}", result.Value);
        TempData["SuccessMessage"] = "Nota criada com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var result = await _noteService.GetByIdAsync(id, cancellationToken);
        if (result.IsFailure)
        {
            return MapFailure(result);
        }

        var categories = await LoadAvailableCategoriesAsync(cancellationToken);
        var note = result.Value;

        return View(new EditNoteViewModel
        {
            Id = note.Id,
            Title = note.Title,
            Description = note.Description,
            Priority = note.Priority,
            SelectedCategoryIds = note.Categories.Select(c => c.Id).ToList(),
            AvailableCategories = categories
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, EditNoteViewModel model, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return NotFound();
        }

        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            model.AvailableCategories = await LoadAvailableCategoriesAsync(cancellationToken);
            return View(model);
        }

        var result = await _noteService.UpdateAsync(
            new UpdateNoteRequest(id, model.Title, model.Description, model.Priority, model.SelectedCategoryIds),
            cancellationToken);

        if (result.IsFailure)
        {
            if (result.ErrorType is ErrorType.Validation or ErrorType.Conflict)
            {
                AddErrorsToModelState(result);
                model.AvailableCategories = await LoadAvailableCategoriesAsync(cancellationToken);
                return View(model);
            }

            return MapFailure(result);
        }

        _logger.LogInformation("Note updated. NoteId: {NoteId}", id);
        TempData["SuccessMessage"] = "Nota atualizada com sucesso.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Pin(Guid id, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        var result = await _noteService.PinAsync(id, cancellationToken);
        if (result.IsFailure)
        {
            if (result.ErrorType == ErrorType.Validation)
            {
                TempData["WarningMessage"] = result.Error;
                return RedirectToLocalOrNotes(returnUrl);
            }

            return MapFailure(result);
        }

        TempData["SuccessMessage"] = "Nota fixada no topo.";
        return RedirectToLocalOrNotes(returnUrl);
    }

    [HttpPost]
    public async Task<IActionResult> Unpin(Guid id, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        var result = await _noteService.UnpinAsync(id, cancellationToken);
        if (result.IsFailure)
        {
            return MapFailure(result);
        }

        TempData["InfoMessage"] = "Nota desafixada.";
        return RedirectToLocalOrNotes(returnUrl);
    }

    [HttpPost]
    public async Task<IActionResult> Archive(Guid id, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        var result = await _noteService.ArchiveAsync(id, cancellationToken);
        if (result.IsFailure)
        {
            return MapFailure(result);
        }

        _logger.LogInformation("Note archived. NoteId: {NoteId}", id);
        TempData["SuccessMessage"] = "Nota arquivada com sucesso.";
        return RedirectToLocalOrNotes(returnUrl);
    }

    [HttpPost]
    public async Task<IActionResult> Unarchive(Guid id, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        var result = await _noteService.UnarchiveAsync(id, cancellationToken);
        if (result.IsFailure)
        {
            return MapFailure(result);
        }

        _logger.LogInformation("Note unarchived. NoteId: {NoteId}", id);
        TempData["SuccessMessage"] = "Nota restaurada para suas notas ativas.";
        return RedirectToLocalOrNotes(returnUrl, defaultAction: nameof(Archived));
    }

    [HttpPost]
    public async Task<IActionResult> MoveToTrash(Guid id, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        var result = await _noteService.MoveToTrashAsync(id, cancellationToken);
        if (result.IsFailure)
        {
            return MapFailure(result);
        }

        _logger.LogInformation("Note moved to trash. NoteId: {NoteId}", id);
        TempData["InfoMessage"] = "Nota movida para a lixeira.";
        return RedirectToLocalOrNotes(returnUrl);
    }

    private async Task<IActionResult> BuildNotesListViewAsync(
        bool isArchived,
        string? searchTerm,
        Guid? categoryId,
        NotePriority? priority,
        NoteSortBy sortBy,
        int page,
        CancellationToken cancellationToken)
    {
        var safePage = Math.Max(1, page);
        var query = new ListNotesQuery(
            IsArchived: isArchived,
            SearchTerm: searchTerm,
            CategoryId: categoryId,
            Priority: priority,
            SortBy: sortBy,
            PageNumber: safePage,
            PageSize: DefaultPageSize);

        var notesResult = await _noteService.ListAsync(query, cancellationToken);
        if (notesResult.IsFailure)
        {
            return MapFailure(notesResult);
        }

        var categories = await LoadAvailableCategoriesAsync(cancellationToken);

        var viewModel = new NoteListViewModel
        {
            IsArchivedView = isArchived,
            Filter = new NoteFilterViewModel
            {
                SearchTerm = searchTerm?.Trim(),
                CategoryId = categoryId,
                Priority = priority,
                SortBy = sortBy,
                Page = notesResult.Value.PageNumber
            },
            Notes = notesResult.Value,
            AvailableCategories = categories
        };

        return View(isArchived ? nameof(Archived) : nameof(Index), viewModel);
    }

    private async Task<IReadOnlyList<CategoryListItemDto>> LoadAvailableCategoriesAsync(
        CancellationToken cancellationToken)
    {
        var categoriesResult = await _categoryService.ListAsync(cancellationToken);
        return categoriesResult.IsSuccess
            ? categoriesResult.Value
            : Array.Empty<CategoryListItemDto>();
    }

    private IActionResult RedirectToLocalOrNotes(string? returnUrl, string defaultAction = nameof(Index))
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction(defaultAction);
    }

    private void AddErrorsToModelState(Result result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }
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
