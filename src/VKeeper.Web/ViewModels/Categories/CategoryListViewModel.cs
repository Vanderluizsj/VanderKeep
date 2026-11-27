namespace VKeeper.Web.ViewModels.Categories;

public sealed class CategoryListViewModel
{
    public IReadOnlyList<CategoryItemViewModel> Categories { get; init; } = Array.Empty<CategoryItemViewModel>();
}
