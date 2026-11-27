using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VKeeper.Aplicacao.Categorias;
using VKeeper.Aplicacao.Categorias.AtualizarCategoria;
using VKeeper.Aplicacao.Categorias.CriarCategoria;
using VKeeper.Aplicacao.Comum;
using VKeeper.Web.ViewModels.Categories;

namespace VKeeper.Web.Controllers;

[Authorize]
public sealed class CategoriesController : Controller
{
    private readonly ICategoryService _categoryService;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(
        ICategoryService categoryService,
        ILogger<CategoriesController> logger)
    {
        _categoryService = categoryService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var result = await _categoryService.ListAsync(cancellationToken);
        if (result.IsFailure)
        {
            return MapFailure(result);
        }

        var viewModel = new CategoryListViewModel
        {
            Categories = result.Value
                .Select(c => new CategoryItemViewModel
                {
                    Id = c.Id,
                    Title = c.Title,
                    NoteCount = c.NoteCount,
                    CreatedAtUtc = c.CreatedAtUtc,
                    UpdatedAtUtc = c.UpdatedAtUtc
                })
                .ToArray()
        };

        return View(viewModel);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateCategoryViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCategoryViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _categoryService.CreateAsync(new CreateCategoryRequest(model.Title), cancellationToken);
        if (result.IsFailure)
        {
            if (result.ErrorType is ErrorType.Validation or ErrorType.Conflict)
            {
                AddErrorsToModelState(result);
                return View(model);
            }

            return MapFailure(result);
        }

        _logger.LogInformation("Category created. CategoryId: {CategoryId}", result.Value);
        TempData["SuccessMessage"] = "Categoria criada com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var result = await _categoryService.GetByIdAsync(id, cancellationToken);
        if (result.IsFailure)
        {
            return MapFailure(result);
        }

        return View(new EditCategoryViewModel
        {
            Id = result.Value.Id,
            Title = result.Value.Title
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, EditCategoryViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _categoryService.UpdateAsync(new UpdateCategoryRequest(id, model.Title), cancellationToken);
        if (result.IsFailure)
        {
            if (result.ErrorType is ErrorType.Validation or ErrorType.Conflict)
            {
                AddErrorsToModelState(result);
                return View(model);
            }

            return MapFailure(result);
        }

        _logger.LogInformation("Category updated. CategoryId: {CategoryId}", id);
        TempData["SuccessMessage"] = "Categoria atualizada com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _categoryService.GetByIdAsync(id, cancellationToken);
        if (result.IsFailure)
        {
            return MapFailure(result);
        }

        return View(new DeleteCategoryViewModel
        {
            Id = result.Value.Id,
            Title = result.Value.Title,
            NoteCount = result.Value.NoteCount
        });
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(Guid id, CancellationToken cancellationToken)
    {
        var result = await _categoryService.DeleteAsync(id, cancellationToken);
        if (result.IsFailure)
        {
            return MapFailure(result);
        }

        _logger.LogInformation("Category deleted. CategoryId: {CategoryId}", id);
        TempData["SuccessMessage"] = "Categoria excluída com sucesso. As notas vinculadas foram preservadas.";
        return RedirectToAction(nameof(Index));
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
