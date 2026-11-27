using Vanderkeep.Dominio.Entidades;

namespace Vanderkeep.Dominio.Interfaces;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Category>> GetByIdsAsync(
        IEnumerable<Guid> ids,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Category>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategorySummary>> ListWithNoteCountByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByNormalizedTitleAsync(
        Guid userId,
        string normalizedTitle,
        Guid? excludeCategoryId = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(Category category, CancellationToken cancellationToken = default);

    void Remove(Category category);
}
