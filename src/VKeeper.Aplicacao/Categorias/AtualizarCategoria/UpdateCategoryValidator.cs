using FluentValidation;
using VKeeper.Dominio.Entidades;

namespace VKeeper.Aplicacao.Categorias.AtualizarCategoria;

public sealed class UpdateCategoryValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("A categoria informada é inválida.");

        RuleFor(x => x.Title)
            .Must(title => !string.IsNullOrWhiteSpace(title))
            .WithMessage("O título da categoria é obrigatório.")
            .Must(title => string.IsNullOrWhiteSpace(title) || title.Trim().Length <= Category.MaxTitleLength)
            .WithMessage($"O título da categoria deve ter no máximo {Category.MaxTitleLength} caracteres.");
    }
}
