using System.ComponentModel.DataAnnotations;

namespace VKeeper.Web.ViewModels.Categories;

public sealed class EditCategoryViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "O título da categoria é obrigatório.")]
    [StringLength(50, ErrorMessage = "O título da categoria deve ter no máximo {1} caracteres.")]
    [Display(Name = "Título")]
    public string Title { get; set; } = string.Empty;
}
