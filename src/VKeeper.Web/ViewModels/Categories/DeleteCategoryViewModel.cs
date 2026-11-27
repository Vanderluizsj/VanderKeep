namespace VKeeper.Web.ViewModels.Categories;

public sealed class DeleteCategoryViewModel
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public int NoteCount { get; init; }
}
