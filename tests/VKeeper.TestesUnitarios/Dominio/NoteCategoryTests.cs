using VKeeper.Dominio.Entidades;
using VKeeper.Dominio.Excecoes;

namespace VKeeper.TestesUnitarios.Dominio;

public sealed class NoteCategoryTests
{
    [Fact]
    public void Constructor_WithValidIdentifiers_ShouldAssignProperties()
    {
        var noteId = Guid.CreateVersion7();
        var categoryId = Guid.CreateVersion7();

        var link = new NoteCategory(noteId, categoryId);

        Assert.Equal(noteId, link.NoteId);
        Assert.Equal(categoryId, link.CategoryId);
    }

    [Fact]
    public void Constructor_WithEmptyNoteId_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => new NoteCategory(Guid.Empty, Guid.CreateVersion7()));
    }

    [Fact]
    public void Constructor_WithEmptyCategoryId_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => new NoteCategory(Guid.CreateVersion7(), Guid.Empty));
    }
}
