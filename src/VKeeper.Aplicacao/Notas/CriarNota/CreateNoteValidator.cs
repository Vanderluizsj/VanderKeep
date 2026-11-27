using FluentValidation;
using VKeeper.Dominio.Entidades;

namespace VKeeper.Aplicacao.Notas.CriarNota;

public sealed class CreateNoteValidator : AbstractValidator<CreateNoteRequest>
{
    public CreateNoteValidator()
    {
        RuleFor(x => x.Title)
            .Must(title => !string.IsNullOrWhiteSpace(title))
            .WithMessage("O título da nota é obrigatório.")
            .Must(title => string.IsNullOrWhiteSpace(title) || title.Trim().Length <= Note.MaxTitleLength)
            .WithMessage($"O título da nota deve ter no máximo {Note.MaxTitleLength} caracteres.");

        RuleFor(x => x.Description)
            .Must(description => string.IsNullOrWhiteSpace(description) || description.Trim().Length <= Note.MaxDescriptionLength)
            .WithMessage($"A descrição da nota deve ter no máximo {Note.MaxDescriptionLength} caracteres.");

        RuleFor(x => x.Priority)
            .IsInEnum()
            .WithMessage("Selecione uma prioridade válida.");

        RuleFor(x => x.CategoryIds)
            .Must(ids => ids is null || ids.All(id => id != Guid.Empty))
            .WithMessage("Uma ou mais categorias selecionadas são inválidas.");
    }
}
