using FluentValidation;
using VKeeper.Dominio.Entidades;

namespace VKeeper.Aplicacao.Categorias.CriarCategoria;

public sealed class CreateCategoryValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Title)
            .Must(title => !string.IsNullOrWhiteSpace(title))
            .WithMessage("O título da categoria é obrigatório.")
            .Must(title => string.IsNullOrWhiteSpace(title) || title.Trim().Length <= Category.MaxTitleLength)
            .WithMessage($"O título da categoria deve ter no máximo {Category.MaxTitleLength} caracteres.");
    }
}
