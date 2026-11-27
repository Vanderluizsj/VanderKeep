using VKeeper.Dominio.Excecoes;

namespace VKeeper.Dominio.Entidades;

public sealed class NoteCategory
{
    private NoteCategory()
    {
    }

    public NoteCategory(Guid noteId, Guid categoryId)
    {
        if (noteId == Guid.Empty)
        {
            throw new DomainException("Note identifier cannot be empty.");
        }

        if (categoryId == Guid.Empty)
        {
            throw new DomainException("Category identifier cannot be empty.");
        }

        NoteId = noteId;
        CategoryId = categoryId;
    }

    public Guid NoteId { get; private set; }

    public Guid CategoryId { get; private set; }

    public Note Note { get; private set; } = null!;

    public Category Category { get; private set; } = null!;
}
