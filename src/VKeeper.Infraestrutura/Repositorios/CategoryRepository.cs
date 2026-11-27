using Microsoft.EntityFrameworkCore;
using VKeeper.Dominio.Entidades;
using VKeeper.Dominio.Interfaces;
using VKeeper.Infraestrutura.Persistencia;

namespace VKeeper.Infraestrutura.Repositorios;

public sealed class CategoryRepository : ICategoryRepository
{
    private readonly VKeeperDbContext _dbContext;

    public CategoryRepository(VKeeperDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Category?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, cancellationToken);
    }

    public Task<CategorySummary?> GetSummaryByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Categories
            .AsNoTracking()
            .Where(c => c.Id == id && c.UserId == userId)
            .Select(c => new CategorySummary(
                c.Id,
                c.Title,
                c.NoteCategories.Count(nc => !nc.Note.IsDeleted && nc.Note.UserId == userId),
                c.CreatedAtUtc,
                c.UpdatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Category>> GetByIdsAsync(
        IEnumerable<Guid> ids,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var idList = ids.Distinct().ToArray();
        if (idList.Length == 0)
        {
            return Array.Empty<Category>();
        }

        return await _dbContext.Categories
            .AsNoTracking()
            .Where(c => c.UserId == userId && idList.Contains(c.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Category>> ListByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Categories
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Title)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CategorySummary>> ListWithNoteCountByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Categories
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Title)
            .Select(c => new CategorySummary(
                c.Id,
                c.Title,
                c.NoteCategories.Count(nc => !nc.Note.IsDeleted && nc.Note.UserId == userId),
                c.CreatedAtUtc,
                c.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByNormalizedTitleAsync(
        Guid userId,
        string normalizedTitle,
        Guid? excludeCategoryId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Categories
            .AsNoTracking()
            .Where(c => c.UserId == userId && c.NormalizedTitle == normalizedTitle);

        if (excludeCategoryId.HasValue)
        {
            query = query.Where(c => c.Id != excludeCategoryId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        return _dbContext.Categories.AddAsync(category, cancellationToken).AsTask();
    }

    public void Remove(Category category)
    {
        _dbContext.Categories.Remove(category);
    }
}
