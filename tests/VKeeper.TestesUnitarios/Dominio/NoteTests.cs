using VKeeper.Dominio.Entidades;
using VKeeper.Dominio.Enums;
using VKeeper.Dominio.Excecoes;

namespace VKeeper.TestesUnitarios.Dominio;

public sealed class NoteTests
{
    private static readonly DateTime CreatedAt = new(2026, 11, 26, 21, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Constructor_WithValidData_ShouldInitializeActiveNote()
    {
        var userId = Guid.CreateVersion7();

        var note = new Note(userId, "  Revisar arquitetura  ", "  Detalhar ADR-13  ", NotePriority.High, CreatedAt);

        Assert.NotEqual(Guid.Empty, note.Id);
        Assert.Equal(7, note.Id.Version);
        Assert.Equal(userId, note.UserId);
        Assert.Equal("Revisar arquitetura", note.Title);
        Assert.Equal("Detalhar ADR-13", note.Description);
        Assert.Equal(NotePriority.High, note.Priority);
        Assert.False(note.IsPinned);
        Assert.False(note.IsArchived);
        Assert.False(note.IsDeleted);
        Assert.Null(note.DeletedAtUtc);
        Assert.Equal(CreatedAt, note.CreatedAtUtc);
        Assert.Equal(CreatedAt, note.UpdatedAtUtc);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithOptionalOrBlankDescription_ShouldStoreNullDescription(string? description)
    {
        var note = new Note(Guid.CreateVersion7(), "Nota rápida", description, NotePriority.Medium, CreatedAt);

        Assert.Null(note.Description);
    }

    [Fact]
    public void Constructor_WithEmptyUserId_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() =>
            new Note(Guid.Empty, "Título", null, NotePriority.Medium, CreatedAt));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankTitle_ShouldThrowDomainException(string title)
    {
        Assert.Throws<DomainException>(() =>
            new Note(Guid.CreateVersion7(), title, null, NotePriority.Medium, CreatedAt));
    }

    [Fact]
    public void Constructor_WhenTitleOrDescriptionExceedMaxLength_ShouldThrowDomainException()
    {
        var longTitle = new string('a', Note.MaxTitleLength + 1);
        var longDescription = new string('b', Note.MaxDescriptionLength + 1);

        Assert.Throws<DomainException>(() =>
            new Note(Guid.CreateVersion7(), longTitle, null, NotePriority.Medium, CreatedAt));
        Assert.Throws<DomainException>(() =>
            new Note(Guid.CreateVersion7(), "Válido", longDescription, NotePriority.Medium, CreatedAt));
    }

    [Fact]
    public void Constructor_WithInvalidPriority_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() =>
            new Note(Guid.CreateVersion7(), "Título", null, (NotePriority)99, CreatedAt));
    }

    [Fact]
    public void UpdateDetails_WithValidData_ShouldUpdatePropertiesAndUpdatedTimestamp()
    {
        var note = CreateSampleNote();
        var updatedAt = CreatedAt.AddMinutes(30);

        note.UpdateDetails("  Novo título  ", "  Nova descrição  ", NotePriority.High, updatedAt);

        Assert.Equal("Novo título", note.Title);
        Assert.Equal("Nova descrição", note.Description);
        Assert.Equal(NotePriority.High, note.Priority);
        Assert.Equal(CreatedAt, note.CreatedAtUtc);
        Assert.Equal(updatedAt, note.UpdatedAtUtc);
    }

    [Fact]
    public void MoveToTrash_FromArchivedNote_ShouldClearArchivedAndSetDeletedAtUtc()
    {
        var note = CreateSampleNote();
        note.Archive(CreatedAt.AddMinutes(2));
        var deletedAt = CreatedAt.AddMinutes(5);

        note.MoveToTrash(deletedAt);

        Assert.True(note.IsDeleted);
        Assert.Equal(deletedAt, note.DeletedAtUtc);
        Assert.False(note.IsArchived);
        Assert.False(note.IsPinned);
    }

    [Fact]
    public void UpdateCategories_ShouldDeduplicateAndApplyIncrementalDiff()
    {
        var note = CreateSampleNote();
        var cat1 = Guid.CreateVersion7();
        var cat2 = Guid.CreateVersion7();
        var cat3 = Guid.CreateVersion7();

        note.UpdateCategories(new[] { cat1, cat2, cat1 }, CreatedAt.AddMinutes(5));
        Assert.Equal(2, note.NoteCategories.Count);
        Assert.Contains(note.NoteCategories, nc => nc.CategoryId == cat1);
        Assert.Contains(note.NoteCategories, nc => nc.CategoryId == cat2);

        note.UpdateCategories(new[] { cat2, cat3 }, CreatedAt.AddMinutes(10));
        Assert.Equal(2, note.NoteCategories.Count);
        Assert.DoesNotContain(note.NoteCategories, nc => nc.CategoryId == cat1);
        Assert.Contains(note.NoteCategories, nc => nc.CategoryId == cat2);
        Assert.Contains(note.NoteCategories, nc => nc.CategoryId == cat3);
        Assert.Equal(CreatedAt.AddMinutes(10), note.UpdatedAtUtc);
    }

    [Fact]
    public void UpdateCategories_WithEmptyCategoryId_ShouldThrowDomainException()
    {
        var note = CreateSampleNote();

        Assert.Throws<DomainException>(() =>
            note.UpdateCategories(new[] { Guid.Empty }, CreatedAt.AddMinutes(1)));
    }

    [Fact]
    public void PinAndArchive_ShouldRespectStateMachineInvariants()
    {
        var note = CreateSampleNote();

        note.Pin(CreatedAt.AddMinutes(2));
        Assert.True(note.IsPinned);

        // Archiving a pinned note must clear IsPinned (RN07).
        note.Archive(CreatedAt.AddMinutes(4));
        Assert.True(note.IsArchived);
        Assert.False(note.IsPinned);

        // Cannot pin while archived.
        Assert.Throws<DomainException>(() => note.Pin(CreatedAt.AddMinutes(5)));

        note.Unarchive(CreatedAt.AddMinutes(6));
        Assert.False(note.IsArchived);
        Assert.False(note.IsPinned);

        note.Pin(CreatedAt.AddMinutes(7));
        Assert.True(note.IsPinned);

        note.Unpin(CreatedAt.AddMinutes(8));
        Assert.False(note.IsPinned);
    }

    [Fact]
    public void MoveToTrashAndRestore_ShouldTransitionStateAndPreserveCategories()
    {
        var note = CreateSampleNote();
        var categoryId = Guid.CreateVersion7();
        note.UpdateCategories(new[] { categoryId }, CreatedAt.AddMinutes(1));
        note.Pin(CreatedAt.AddMinutes(2));

        var deletedAt = CreatedAt.AddMinutes(5);
        note.MoveToTrash(deletedAt);

        Assert.True(note.IsDeleted);
        Assert.Equal(deletedAt, note.DeletedAtUtc);
        Assert.False(note.IsPinned);
        Assert.False(note.IsArchived);
        Assert.Single(note.NoteCategories);

        // Operations forbidden while in trash.
        Assert.Throws<DomainException>(() => note.UpdateDetails("Novo", null, NotePriority.Low, deletedAt.AddMinutes(1)));
        Assert.Throws<DomainException>(() => note.UpdateCategories(Array.Empty<Guid>(), deletedAt.AddMinutes(1)));
        Assert.Throws<DomainException>(() => note.Pin(deletedAt.AddMinutes(1)));
        Assert.Throws<DomainException>(() => note.Unpin(deletedAt.AddMinutes(1)));
        Assert.Throws<DomainException>(() => note.Archive(deletedAt.AddMinutes(1)));
        Assert.Throws<DomainException>(() => note.Unarchive(deletedAt.AddMinutes(1)));
        Assert.Throws<DomainException>(() => note.MoveToTrash(deletedAt.AddMinutes(1)));

        var restoredAt = deletedAt.AddMinutes(10);
        note.Restore(restoredAt);

        Assert.False(note.IsDeleted);
        Assert.Null(note.DeletedAtUtc);
        Assert.False(note.IsArchived);
        Assert.False(note.IsPinned);
        Assert.Equal(restoredAt, note.UpdatedAtUtc);
        Assert.Single(note.NoteCategories);
        Assert.Equal(categoryId, note.NoteCategories.Single().CategoryId);
    }

    [Fact]
    public void Constructor_WithExactBoundaryLengths_ShouldSucceed()
    {
        var maxTitle = new string('a', Note.MaxTitleLength);
        var maxDescription = new string('b', Note.MaxDescriptionLength);

        var note = new Note(Guid.CreateVersion7(), maxTitle, maxDescription, NotePriority.Low, CreatedAt);

        Assert.Equal(Note.MaxTitleLength, note.Title.Length);
        Assert.Equal(Note.MaxDescriptionLength, note.Description!.Length);
    }

    [Fact]
    public void UpdateDetails_OnArchivedNote_ShouldPreserveArchivedState()
    {
        var note = CreateSampleNote();
        note.Archive(CreatedAt.AddMinutes(1));
        var updatedAt = CreatedAt.AddMinutes(5);

        note.UpdateDetails("Título atualizado", "Descrição", NotePriority.Low, updatedAt);

        Assert.True(note.IsArchived);
        Assert.False(note.IsPinned);
        Assert.Equal("Título atualizado", note.Title);
        Assert.Equal(updatedAt, note.UpdatedAtUtc);
    }

    [Fact]
    public void UpdateCategories_WhenClearedOrNull_ShouldClearLinksOrThrow()
    {
        var note = CreateSampleNote();
        note.UpdateCategories(new[] { Guid.CreateVersion7() }, CreatedAt.AddMinutes(1));

        note.UpdateCategories(Array.Empty<Guid>(), CreatedAt.AddMinutes(2));
        Assert.Empty(note.NoteCategories);

        Assert.Throws<ArgumentNullException>(() => note.UpdateCategories(null!, CreatedAt.AddMinutes(3)));
    }

    [Fact]
    public void StateTransitions_WhenAlreadyInTargetState_ShouldNotModifyUpdatedAtUtc()
    {
        var note = CreateSampleNote();

        note.Unpin(CreatedAt.AddMinutes(1));
        note.Unarchive(CreatedAt.AddMinutes(2));
        Assert.Equal(CreatedAt, note.UpdatedAtUtc);

        var pinnedAt = CreatedAt.AddMinutes(3);
        note.Pin(pinnedAt);
        note.Pin(pinnedAt.AddMinutes(1));
        Assert.Equal(pinnedAt, note.UpdatedAtUtc);

        var archivedAt = CreatedAt.AddMinutes(5);
        note.Archive(archivedAt);
        note.Archive(archivedAt.AddMinutes(1));
        Assert.Equal(archivedAt, note.UpdatedAtUtc);
    }

    [Fact]
    public void Restore_WhenNoteIsNotInTrash_ShouldThrowDomainException()
    {
        var note = CreateSampleNote();

        Assert.Throws<DomainException>(() => note.Restore(CreatedAt.AddMinutes(1)));
    }

    private static Note CreateSampleNote() =>
        new(Guid.CreateVersion7(), "Nota inicial", "Descrição inicial", NotePriority.Medium, CreatedAt);
}
