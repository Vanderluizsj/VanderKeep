using System.ComponentModel.DataAnnotations;
using VKeeper.Aplicacao.Categorias.ListarCategorias;
using VKeeper.Dominio.Entidades;
using VKeeper.Dominio.Enums;

namespace VKeeper.Web.ViewModels.Notes;

public sealed class CreateNoteViewModel
{
    [Required(ErrorMessage = "O título da nota é obrigatório.")]
    [StringLength(Note.MaxTitleLength, ErrorMessage = "O título da nota deve ter no máximo {1} caracteres.")]
    [Display(Name = "Título")]
    public string Title { get; set; } = string.Empty;

    [StringLength(Note.MaxDescriptionLength, ErrorMessage = "A descrição da nota deve ter no máximo {1} caracteres.")]
    [Display(Name = "Descrição")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "A prioridade é obrigatória.")]
    [EnumDataType(typeof(NotePriority), ErrorMessage = "Selecione uma prioridade válida.")]
    [Display(Name = "Prioridade")]
    public NotePriority Priority { get; set; } = NotePriority.Medium;

    [Display(Name = "Categorias")]
    public List<Guid> SelectedCategoryIds { get; set; } = [];

    public IReadOnlyList<CategoryListItemDto> AvailableCategories { get; set; } =
        Array.Empty<CategoryListItemDto>();
}

public sealed class EditNoteViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "O título da nota é obrigatório.")]
    [StringLength(Note.MaxTitleLength, ErrorMessage = "O título da nota deve ter no máximo {1} caracteres.")]
    [Display(Name = "Título")]
    public string Title { get; set; } = string.Empty;

    [StringLength(Note.MaxDescriptionLength, ErrorMessage = "A descrição da nota deve ter no máximo {1} caracteres.")]
    [Display(Name = "Descrição")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "A prioridade é obrigatória.")]
    [EnumDataType(typeof(NotePriority), ErrorMessage = "Selecione uma prioridade válida.")]
    [Display(Name = "Prioridade")]
    public NotePriority Priority { get; set; } = NotePriority.Medium;

    [Display(Name = "Categorias")]
    public List<Guid> SelectedCategoryIds { get; set; } = [];

    public IReadOnlyList<CategoryListItemDto> AvailableCategories { get; set; } =
        Array.Empty<CategoryListItemDto>();
}
