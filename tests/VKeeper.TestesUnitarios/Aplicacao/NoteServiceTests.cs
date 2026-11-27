using VKeeper.Aplicacao.Comum;
using VKeeper.Aplicacao.Notas;
using VKeeper.Aplicacao.Notas.AtualizarNota;
using VKeeper.Aplicacao.Notas.CriarNota;
using VKeeper.Aplicacao.Notas.ListarNotas;
using VKeeper.Aplicacao.Notas.Lixeira;
using VKeeper.Dominio.Entidades;
using VKeeper.Dominio.Enums;
using VKeeper.Dominio.Interfaces;

namespace VKeeper.TestesUnitarios.Aplicacao;

public sealed class NoteServiceTests
{
    private static readonly DateTime FixedUtcNow = new(2026, 11, 27, 14, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateAsync_WithOwnedCategories_ShouldPersistNoteAndAssociations()
    {
        var userId = Guid.CreateVersion7();
        var (service, noteRepo, categoryRepo, uow) = CreateSut(userId);
        var cat1 = new Category(userId, "Trabalho", FixedUtcNow);
        var cat2 = new Category(userId, "Estudos", FixedUtcNow);
        categoryRepo.Categories.AddRange([cat1, cat2]);

        var result = await service.CreateAsync(new CreateNoteRequest(
            "  Arquitetura  ",
            "  Revisar Clean Architecture  ",
            NotePriority.High,
            new[] { cat1.Id, cat2.Id, cat1.Id }));

        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Value.Version);
        Assert.Equal(1, uow.SaveCalls);

        var saved = Assert.Single(noteRepo.Notes);
        Assert.Equal("Arquitetura", saved.Title);
        Assert.Equal("Revisar Clean Architecture", saved.Description);
        Assert.Equal(NotePriority.High, saved.Priority);
        Assert.Equal(2, saved.NoteCategories.Count);
    }

    [Fact]
    public async Task CreateAsync_WithCategoryOwnedByAnotherUser_ShouldRejectWithValidationFailure()
    {
        var userA = Guid.CreateVersion7();
        var userB = Guid.CreateVersion7();
        var (service, noteRepo, categoryRepo, uow) = CreateSut(userA);
        var catOfB = new Category(userB, "Privada de B", FixedUtcNow);
        categoryRepo.Categories.Add(catOfB);

        var result = await service.CreateAsync(new CreateNoteRequest(
            "Nota de A",
            null,
            NotePriority.Medium,
            new[] { catOfB.Id }));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Empty(noteRepo.Notes);
        Assert.Equal(0, uow.SaveCalls);
    }

    [Fact]
    public async Task CreateAsync_WhenUnauthenticatedOrInvalidInput_ShouldFailWithoutSaving()
    {
        var (anonService, _, _, anonUow) = CreateSut(userId: null);
        var anonResult = await anonService.CreateAsync(new CreateNoteRequest("Título", null, NotePriority.Medium, null));
        Assert.Equal(ErrorType.Forbidden, anonResult.ErrorType);
        Assert.Equal(0, anonUow.SaveCalls);

        var (authService, _, _, authUow) = CreateSut(Guid.CreateVersion7());
        var invalidResult = await authService.CreateAsync(new CreateNoteRequest("   ", null, (NotePriority)99, null));
        Assert.Equal(ErrorType.Validation, invalidResult.ErrorType);
        Assert.Equal(0, authUow.SaveCalls);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateDetailsAndCategoriesAndRejectForeignCategory()
    {
        var userA = Guid.CreateVersion7();
        var userB = Guid.CreateVersion7();
        var (service, noteRepo, categoryRepo, uow) = CreateSut(userA);
        var catA = new Category(userA, "A", FixedUtcNow);
        var catB = new Category(userB, "B", FixedUtcNow);
        categoryRepo.Categories.AddRange([catA, catB]);

        var note = new Note(userA, "Original", "Desc", NotePriority.Low, FixedUtcNow.AddHours(-1));
        noteRepo.Notes.Add(note);

        // Foreign category must be rejected.
        var foreignAttempt = await service.UpdateAsync(new UpdateNoteRequest(
            note.Id, "Atualizada", "Nova", NotePriority.High, new[] { catB.Id }));
        Assert.Equal(ErrorType.Validation, foreignAttempt.ErrorType);
        Assert.Equal("Original", note.Title);
        Assert.Equal(0, uow.SaveCalls);

        // Valid update must succeed.
        var validUpdate = await service.UpdateAsync(new UpdateNoteRequest(
            note.Id, "Atualizada", "Nova", NotePriority.High, new[] { catA.Id }));
        Assert.True(validUpdate.IsSuccess);
        Assert.Equal("Atualizada", note.Title);
        Assert.Equal(NotePriority.High, note.Priority);
        Assert.Single(note.NoteCategories);
        Assert.Equal(1, uow.SaveCalls);
    }

    [Fact]
    public async Task UpdateAsync_WhenNoteInTrashOrEmptyIdOrForeignOwner_ShouldReturnNotFound()
    {
        var userA = Guid.CreateVersion7();
        var userB = Guid.CreateVersion7();
        var (service, noteRepo, _, uow) = CreateSut(userA);

        var trashedNote = new Note(userA, "Na Lixeira", null, NotePriority.Medium, FixedUtcNow);
        trashedNote.MoveToTrash(FixedUtcNow);
        var foreignNote = new Note(userB, "De B", null, NotePriority.Medium, FixedUtcNow);
        noteRepo.Notes.AddRange([trashedNote, foreignNote]);

        var trashResult = await service.UpdateAsync(new UpdateNoteRequest(trashedNote.Id, "X", null, NotePriority.Low, null));
        var foreignResult = await service.UpdateAsync(new UpdateNoteRequest(foreignNote.Id, "X", null, NotePriority.Low, null));
        var emptyResult = await service.UpdateAsync(new UpdateNoteRequest(Guid.Empty, "X", null, NotePriority.Low, null));

        Assert.Equal(ErrorType.NotFound, trashResult.ErrorType);
        Assert.Equal(ErrorType.NotFound, foreignResult.ErrorType);
        Assert.Equal(ErrorType.NotFound, emptyResult.ErrorType);
        Assert.Equal(0, uow.SaveCalls);
    }

    [Fact]
    public async Task PinArchiveTrashRestoreAndDeletePermanently_ShouldEnforceStateMachine()
    {
        var userId = Guid.CreateVersion7();
        var (service, noteRepo, _, uow) = CreateSut(userId);
        var note = new Note(userId, "Ciclo de vida", null, NotePriority.Medium, FixedUtcNow);
        noteRepo.Notes.Add(note);

        // Active -> Pin -> Archive (clears pin).
        Assert.True((await service.PinAsync(note.Id)).IsSuccess);
        Assert.True(note.IsPinned);

        Assert.True((await service.ArchiveAsync(note.Id)).IsSuccess);
        Assert.True(note.IsArchived);
        Assert.False(note.IsPinned);

        // Pin on archived must return ValidationFailure.
        var pinArchived = await service.PinAsync(note.Id);
        Assert.Equal(ErrorType.Validation, pinArchived.ErrorType);

        // Cannot permanently delete a note that is not in the trash (RN07).
        Assert.Equal(ErrorType.NotFound, (await service.DeletePermanentlyAsync(note.Id)).ErrorType);

        // MoveToTrash -> Restore -> MoveToTrash -> DeletePermanently.
        Assert.True((await service.MoveToTrashAsync(note.Id)).IsSuccess);
        Assert.True(note.IsDeleted);
        Assert.False(note.IsArchived);

        Assert.True((await service.RestoreAsync(note.Id)).IsSuccess);
        Assert.False(note.IsDeleted);
        Assert.False(note.IsArchived);

        Assert.True((await service.MoveToTrashAsync(note.Id)).IsSuccess);
        Assert.True((await service.DeletePermanentlyAsync(note.Id)).IsSuccess);
        Assert.Empty(noteRepo.Notes);
        Assert.True(uow.SaveCalls >= 6);
    }

    [Fact]
    public async Task EmptyTrashAsync_ShouldRemoveOnlyAuthenticatedUsersDeletedNotes()
    {
        var userA = Guid.CreateVersion7();
        var userB = Guid.CreateVersion7();
        var (service, noteRepo, _, uow) = CreateSut(userA);

        var activeA = new Note(userA, "Ativa A", null, NotePriority.Medium, FixedUtcNow);
        var trashA1 = new Note(userA, "Lixo A1", null, NotePriority.Low, FixedUtcNow);
        trashA1.MoveToTrash(FixedUtcNow);
        var trashA2 = new Note(userA, "Lixo A2", null, NotePriority.High, FixedUtcNow);
        trashA2.MoveToTrash(FixedUtcNow);
        var trashB = new Note(userB, "Lixo B", null, NotePriority.Medium, FixedUtcNow);
        trashB.MoveToTrash(FixedUtcNow);

        noteRepo.Notes.AddRange([activeA, trashA1, trashA2, trashB]);

        var result = await service.EmptyTrashAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value);
        Assert.Equal(1, uow.SaveCalls);
        Assert.Equal(2, noteRepo.Notes.Count);
        Assert.Contains(noteRepo.Notes, n => n.Id == activeA.Id);
        Assert.Contains(noteRepo.Notes, n => n.Id == trashB.Id);
    }

    [Fact]
    public async Task ListAsyncAndListTrashAsync_ShouldTruncateLongPreviewAndReturnPagedResult()
    {
        var userId = Guid.CreateVersion7();
        var (service, noteRepo, _, _) = CreateSut(userId);
        var longDesc = new string('x', 220);
        var active = new Note(userId, "Nota longa", longDesc, NotePriority.Medium, FixedUtcNow);
        var trashed = new Note(userId, "Nota na lixeira", "Resumo", NotePriority.Low, FixedUtcNow);
        trashed.MoveToTrash(FixedUtcNow);
        noteRepo.Notes.AddRange([active, trashed]);

        var activeResult = await service.ListAsync(new ListNotesQuery(IsArchived: false));
        var trashResult = await service.ListTrashAsync(new ListTrashQuery());

        Assert.True(activeResult.IsSuccess);
        var activeItem = Assert.Single(activeResult.Value.Items);
        Assert.NotNull(activeItem.DescriptionPreview);
        Assert.EndsWith("...", activeItem.DescriptionPreview);
        Assert.True(activeItem.DescriptionPreview!.Length <= 183);

        Assert.True(trashResult.IsSuccess);
        var trashItem = Assert.Single(trashResult.Value.Items);
        Assert.Equal(trashed.Id, trashItem.Id);
    }

    private static (NoteService Service, FakeNoteRepository NoteRepo, FakeCategoryRepository CategoryRepo, FakeUnitOfWork Uow)
        CreateSut(Guid? userId)
    {
        var noteRepo = new FakeNoteRepository();
        var categoryRepo = new FakeCategoryRepository();
        var uow = new FakeUnitOfWork();

        var service = new NoteService(
            noteRepo,
            categoryRepo,
            uow,
            new FakeCurrentUserService(userId),
            new FakeDateTimeProvider(FixedUtcNow),
            new CreateNoteValidator(),
            new UpdateNoteValidator());

        return (service, noteRepo, categoryRepo, uow);
    }

    private sealed class FakeCurrentUserService(Guid? userId) : ICurrentUserService
    {
        public Guid? UserId => userId;
        public bool IsAuthenticated => userId.HasValue;
    }

    private sealed class FakeDateTimeProvider(DateTime utcNow) : IDateTimeProvider
    {
        public DateTime UtcNow => utcNow;
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCalls { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            return Task.FromResult(1);
        }
    }

    private sealed class FakeNoteRepository : INoteRepository
    {
        public List<Note> Notes { get; } = [];

        public Task<Note?> GetByIdAsync(
            Guid id,
            Guid userId,
            bool includeCategories = true,
            bool includeDeleted = false,
            bool asNoTracking = false,
            CancellationToken cancellationToken = default)
        {
            var note = Notes.FirstOrDefault(n =>
                n.Id == id &&
                n.UserId == userId &&
                (includeDeleted || !n.IsDeleted));
            return Task.FromResult(note);
        }

        public Task<(IReadOnlyList<Note> Items, int TotalCount)> ListPagedAsync(
            NoteQueryFilter filter,
            CancellationToken cancellationToken = default)
        {
            var query = Notes.Where(n => n.UserId == filter.UserId && n.IsDeleted == filter.IsDeleted);
            if (!filter.IsDeleted && filter.IsArchived.HasValue)
            {
                query = query.Where(n => n.IsArchived == filter.IsArchived.Value);
            }

            var list = query.ToList();
            IReadOnlyList<Note> page = list
                .Skip((Math.Max(1, filter.PageNumber) - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToArray();

            return Task.FromResult((page, list.Count));
        }

        public Task<IReadOnlyList<Note>> ListDeletedByUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Note> deleted = Notes.Where(n => n.UserId == userId && n.IsDeleted).ToArray();
            return Task.FromResult(deleted);
        }

        public Task AddAsync(Note note, CancellationToken cancellationToken = default)
        {
            Notes.Add(note);
            return Task.CompletedTask;
        }

        public void Remove(Note note) => Notes.Remove(note);

        public void RemoveRange(IEnumerable<Note> notes)
        {
            foreach (var note in notes.ToList())
            {
                Notes.Remove(note);
            }
        }
    }

    private sealed class FakeCategoryRepository : ICategoryRepository
    {
        public List<Category> Categories { get; } = [];

        public Task<Category?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Categories.FirstOrDefault(c => c.Id == id && c.UserId == userId));

        public Task<CategorySummary?> GetSummaryByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<CategorySummary?>(null);

        public Task<IReadOnlyList<Category>> GetByIdsAsync(
            IEnumerable<Guid> ids,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var set = ids.ToHashSet();
            IReadOnlyList<Category> result = Categories.Where(c => c.UserId == userId && set.Contains(c.Id)).ToArray();
            return Task.FromResult(result);
        }

        public Task<IReadOnlyList<Category>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Category>>(Categories.Where(c => c.UserId == userId).ToArray());

        public Task<IReadOnlyList<CategorySummary>> ListWithNoteCountByUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CategorySummary>>(Array.Empty<CategorySummary>());

        public Task<bool> ExistsByNormalizedTitleAsync(Guid userId, string normalizedTitle, Guid? excludeCategoryId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task AddAsync(Category category, CancellationToken cancellationToken = default)
        {
            Categories.Add(category);
            return Task.CompletedTask;
        }

        public void Remove(Category category) => Categories.Remove(category);
    }
}
