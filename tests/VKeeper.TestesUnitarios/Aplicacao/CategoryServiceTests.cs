using VKeeper.Aplicacao.Categorias;
using VKeeper.Aplicacao.Categorias.AtualizarCategoria;
using VKeeper.Aplicacao.Categorias.CriarCategoria;
using VKeeper.Aplicacao.Comum;
using VKeeper.Dominio.Entidades;
using VKeeper.Dominio.Interfaces;

namespace VKeeper.TestesUnitarios.Aplicacao;

public sealed class CategoryServiceTests
{
    private static readonly DateTime FixedUtcNow = new(2026, 11, 27, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateAsync_WithValidUniqueTitle_ShouldPersistAndReturnId()
    {
        var userId = Guid.CreateVersion7();
        var (service, repo, uow) = CreateSut(userId);

        var result = await service.CreateAsync(new CreateCategoryRequest("  Estudos .NET  "));

        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Value.Version);
        Assert.Equal(1, uow.SaveCalls);
        var saved = Assert.Single(repo.Categories);
        Assert.Equal(userId, saved.UserId);
        Assert.Equal("Estudos .NET", saved.Title);
        Assert.Equal("ESTUDOS .NET", saved.NormalizedTitle);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_WithBlankTitle_ShouldReturnValidationFailure(string invalidTitle)
    {
        var (service, repo, uow) = CreateSut(Guid.CreateVersion7());

        var result = await service.CreateAsync(new CreateCategoryRequest(invalidTitle));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Empty(repo.Categories);
        Assert.Equal(0, uow.SaveCalls);
    }

    [Fact]
    public async Task CreateAsync_WhenTitleExceedsMaxLength_ShouldReturnValidationFailure()
    {
        var (service, repo, uow) = CreateSut(Guid.CreateVersion7());
        var longTitle = new string('a', Category.MaxTitleLength + 1);

        var result = await service.CreateAsync(new CreateCategoryRequest(longTitle));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Empty(repo.Categories);
        Assert.Equal(0, uow.SaveCalls);
    }

    [Fact]
    public async Task CreateAsync_WhenDuplicateTitleForSameUser_ShouldReturnConflict()
    {
        var userId = Guid.CreateVersion7();
        var (service, repo, uow) = CreateSut(userId);
        repo.Categories.Add(new Category(userId, "Trabalho", FixedUtcNow));

        var result = await service.CreateAsync(new CreateCategoryRequest("  trabalho  "));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(0, uow.SaveCalls);
    }

    [Fact]
    public async Task CreateAsync_WhenSameTitleExistsOnlyForAnotherUser_ShouldSucceed()
    {
        var userA = Guid.CreateVersion7();
        var userB = Guid.CreateVersion7();
        var (service, repo, uow) = CreateSut(userB);
        repo.Categories.Add(new Category(userA, "Trabalho", FixedUtcNow));

        var result = await service.CreateAsync(new CreateCategoryRequest("Trabalho"));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, uow.SaveCalls);
        Assert.Equal(2, repo.Categories.Count);
    }

    [Fact]
    public async Task CreateAsync_WhenUnauthenticated_ShouldReturnForbidden()
    {
        var (service, _, uow) = CreateSut(userId: null);

        var result = await service.CreateAsync(new CreateCategoryRequest("Pessoal"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        Assert.Equal(0, uow.SaveCalls);
    }

    [Fact]
    public async Task UpdateAsync_WithValidNewTitle_ShouldRenameAndSave()
    {
        var userId = Guid.CreateVersion7();
        var (service, repo, uow) = CreateSut(userId);
        var category = new Category(userId, "Antiga", FixedUtcNow.AddHours(-1));
        repo.Categories.Add(category);

        var result = await service.UpdateAsync(new UpdateCategoryRequest(category.Id, "  Nova  "));

        Assert.True(result.IsSuccess);
        Assert.Equal("Nova", category.Title);
        Assert.Equal("NOVA", category.NormalizedTitle);
        Assert.Equal(FixedUtcNow, category.UpdatedAtUtc);
        Assert.Equal(1, uow.SaveCalls);
    }

    [Fact]
    public async Task UpdateAsync_WhenOnlyChangingCasingOfSameCategory_ShouldSucceed()
    {
        var userId = Guid.CreateVersion7();
        var (service, repo, uow) = CreateSut(userId);
        var category = new Category(userId, "estudos", FixedUtcNow.AddHours(-1));
        repo.Categories.Add(category);

        var result = await service.UpdateAsync(new UpdateCategoryRequest(category.Id, "Estudos"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Estudos", category.Title);
        Assert.Equal(1, uow.SaveCalls);
    }

    [Fact]
    public async Task UpdateAsync_WhenTitleConflictsWithAnotherCategoryOfSameUser_ShouldReturnConflict()
    {
        var userId = Guid.CreateVersion7();
        var (service, repo, uow) = CreateSut(userId);
        var cat1 = new Category(userId, "Trabalho", FixedUtcNow);
        var cat2 = new Category(userId, "Estudos", FixedUtcNow);
        repo.Categories.Add(cat1);
        repo.Categories.Add(cat2);

        var result = await service.UpdateAsync(new UpdateCategoryRequest(cat2.Id, "  TRABALHO "));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal("Estudos", cat2.Title);
        Assert.Equal(0, uow.SaveCalls);
    }

    [Fact]
    public async Task UpdateAsync_WhenCategoryBelongsToAnotherUser_ShouldReturnNotFound()
    {
        var owner = Guid.CreateVersion7();
        var attacker = Guid.CreateVersion7();
        var (service, repo, uow) = CreateSut(attacker);
        var category = new Category(owner, "Privada", FixedUtcNow);
        repo.Categories.Add(category);

        var result = await service.UpdateAsync(new UpdateCategoryRequest(category.Id, "Alterada"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal("Privada", category.Title);
        Assert.Equal(0, uow.SaveCalls);
    }

    [Fact]
    public async Task DeleteAsync_WhenOwnedByUser_ShouldRemoveAndSave()
    {
        var userId = Guid.CreateVersion7();
        var (service, repo, uow) = CreateSut(userId);
        var category = new Category(userId, "Temporária", FixedUtcNow);
        repo.Categories.Add(category);

        var result = await service.DeleteAsync(category.Id);

        Assert.True(result.IsSuccess);
        Assert.Empty(repo.Categories);
        Assert.Equal(1, uow.SaveCalls);
    }

    [Fact]
    public async Task DeleteAsync_WhenOwnedByAnotherUserOrEmpty_ShouldReturnNotFound()
    {
        var owner = Guid.CreateVersion7();
        var attacker = Guid.CreateVersion7();
        var (service, repo, uow) = CreateSut(attacker);
        var category = new Category(owner, "Privada", FixedUtcNow);
        repo.Categories.Add(category);

        var idorResult = await service.DeleteAsync(category.Id);
        var emptyResult = await service.DeleteAsync(Guid.Empty);

        Assert.Equal(ErrorType.NotFound, idorResult.ErrorType);
        Assert.Equal(ErrorType.NotFound, emptyResult.ErrorType);
        Assert.Single(repo.Categories);
        Assert.Equal(0, uow.SaveCalls);
    }

    [Fact]
    public async Task ListAsyncAndGetByIdAsync_ShouldIsolateByAuthenticatedUser()
    {
        var userA = Guid.CreateVersion7();
        var userB = Guid.CreateVersion7();
        var (service, repo, _) = CreateSut(userA);
        var catA = new Category(userA, "A-Categoria", FixedUtcNow);
        var catB = new Category(userB, "B-Categoria", FixedUtcNow);
        repo.Categories.Add(catA);
        repo.Categories.Add(catB);

        var listResult = await service.ListAsync();
        var ownResult = await service.GetByIdAsync(catA.Id);
        var otherResult = await service.GetByIdAsync(catB.Id);

        Assert.True(listResult.IsSuccess);
        var item = Assert.Single(listResult.Value);
        Assert.Equal(catA.Id, item.Id);
        Assert.True(ownResult.IsSuccess);
        Assert.Equal(ErrorType.NotFound, otherResult.ErrorType);
    }

    private static (CategoryService Service, FakeCategoryRepository Repo, FakeUnitOfWork Uow) CreateSut(Guid? userId)
    {
        var repo = new FakeCategoryRepository();
        var uow = new FakeUnitOfWork();
        var currentUser = new FakeCurrentUserService(userId);
        var dateTime = new FakeDateTimeProvider(FixedUtcNow);

        var service = new CategoryService(
            repo,
            uow,
            currentUser,
            dateTime,
            new CreateCategoryValidator(),
            new UpdateCategoryValidator());

        return (service, repo, uow);
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

    private sealed class FakeCategoryRepository : ICategoryRepository
    {
        public List<Category> Categories { get; } = [];

        public Task<Category?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Categories.FirstOrDefault(c => c.Id == id && c.UserId == userId));
        }

        public Task<CategorySummary?> GetSummaryByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
        {
            var category = Categories.FirstOrDefault(c => c.Id == id && c.UserId == userId);
            return Task.FromResult(category is null
                ? null
                : new CategorySummary(category.Id, category.Title, 0, category.CreatedAtUtc, category.UpdatedAtUtc));
        }

        public Task<IReadOnlyList<Category>> GetByIdsAsync(
            IEnumerable<Guid> ids,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var set = ids.ToHashSet();
            IReadOnlyList<Category> result = Categories.Where(c => c.UserId == userId && set.Contains(c.Id)).ToArray();
            return Task.FromResult(result);
        }

        public Task<IReadOnlyList<Category>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Category> result = Categories.Where(c => c.UserId == userId).OrderBy(c => c.Title).ToArray();
            return Task.FromResult(result);
        }

        public Task<IReadOnlyList<CategorySummary>> ListWithNoteCountByUserAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<CategorySummary> result = Categories
                .Where(c => c.UserId == userId)
                .OrderBy(c => c.Title)
                .Select(c => new CategorySummary(c.Id, c.Title, 0, c.CreatedAtUtc, c.UpdatedAtUtc))
                .ToArray();
            return Task.FromResult(result);
        }

        public Task<bool> ExistsByNormalizedTitleAsync(
            Guid userId,
            string normalizedTitle,
            Guid? excludeCategoryId = null,
            CancellationToken cancellationToken = default)
        {
            var exists = Categories.Any(c =>
                c.UserId == userId &&
                c.NormalizedTitle == normalizedTitle &&
                (!excludeCategoryId.HasValue || c.Id != excludeCategoryId.Value));
            return Task.FromResult(exists);
        }

        public Task AddAsync(Category category, CancellationToken cancellationToken = default)
        {
            Categories.Add(category);
            return Task.CompletedTask;
        }

        public void Remove(Category category)
        {
            Categories.Remove(category);
        }
    }
}
