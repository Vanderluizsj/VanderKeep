using VKeeper.Aplicacao.Categorias.ListarCategorias;
using VKeeper.Aplicacao.Comum;
using VKeeper.Aplicacao.Notas.ListarNotas;
using VKeeper.Dominio.Enums;

namespace VKeeper.Web.ViewModels.Notes;

public sealed class NoteFilterViewModel
{
    public string? SearchTerm { get; set; }

    public Guid? CategoryId { get; set; }

    public NotePriority? Priority { get; set; }

    public NoteSortBy SortBy { get; set; } = NoteSortBy.UpdatedAtDesc;

    public int Page { get; set; } = 1;

    public bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(SearchTerm) ||
        CategoryId.HasValue ||
        Priority.HasValue ||
        SortBy != NoteSortBy.UpdatedAtDesc;
}

public sealed class NoteListViewModel
{
    public bool IsArchivedView { get; init; }

    public NoteFilterViewModel Filter { get; init; } = new();

    public PagedResult<NoteListItemDto> Notes { get; init; } =
        new(Array.Empty<NoteListItemDto>(), 1, 12, 0);

    public IReadOnlyList<CategoryListItemDto> AvailableCategories { get; init; } =
        Array.Empty<CategoryListItemDto>();
}
