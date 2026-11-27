namespace VKeeper.Web.ViewModels.Categories;

public sealed class CategoryItemViewModel
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public int NoteCount { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime UpdatedAtUtc { get; init; }
}
