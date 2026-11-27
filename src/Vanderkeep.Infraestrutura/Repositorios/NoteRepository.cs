using Microsoft.EntityFrameworkCore;
using Vanderkeep.Dominio.Entidades;
using Vanderkeep.Dominio.Enums;
using Vanderkeep.Dominio.Interfaces;
using Vanderkeep.Infraestrutura.Persistencia;

namespace Vanderkeep.Infraestrutura.Repositorios;

public sealed class NoteRepository : INoteRepository
{
    private readonly VanderkeepDbContext _dbContext;

    public NoteRepository(VanderkeepDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Note?> GetByIdAsync(
        Guid id,
        Guid userId,
        bool includeCategories = true,
        bool includeDeleted = false,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Note> query = _dbContext.Notes;

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        if (includeCategories)
        {
            query = query
                .Include(n => n.NoteCategories)
                .ThenInclude(nc => nc.Category);
        }

        query = query.Where(n => n.Id == id && n.UserId == userId);

        if (!includeDeleted)
        {
            query = query.Where(n => !n.IsDeleted);
        }

        return query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Note> Items, int TotalCount)> ListPagedAsync(
        NoteQueryFilter filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var pageNumber = Math.Max(1, filter.PageNumber);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        IQueryable<Note> query = _dbContext.Notes
            .AsNoTracking()
            .Where(n => n.UserId == filter.UserId && n.IsDeleted == filter.IsDeleted);

        if (!filter.IsDeleted && filter.IsArchived.HasValue)
        {
            query = query.Where(n => n.IsArchived == filter.IsArchived.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim();
            query = query.Where(n =>
                n.Title.Contains(term) ||
                (n.Description != null && n.Description.Contains(term)));
        }

        if (filter.CategoryId.HasValue)
        {
            var categoryId = filter.CategoryId.Value;
            query = query.Where(n => n.NoteCategories.Any(nc => nc.CategoryId == categoryId));
        }

        if (filter.Priority.HasValue)
        {
            var priority = filter.Priority.Value;
            query = query.Where(n => n.Priority == priority);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        if (totalCount == 0)
        {
            return (Array.Empty<Note>(), 0);
        }

        IOrderedQueryable<Note> orderedQuery = ApplyOrdering(query, filter);

        var items = await orderedQuery
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Include(n => n.NoteCategories)
            .ThenInclude(nc => nc.Category)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<Note>> ListDeletedByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Notes
            .Where(n => n.UserId == userId && n.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(Note note, CancellationToken cancellationToken = default)
    {
        return _dbContext.Notes.AddAsync(note, cancellationToken).AsTask();
    }

    public void Remove(Note note)
    {
        _dbContext.Notes.Remove(note);
    }

    public void RemoveRange(IEnumerable<Note> notes)
    {
        _dbContext.Notes.RemoveRange(notes);
    }

    private static IOrderedQueryable<Note> ApplyOrdering(IQueryable<Note> query, NoteQueryFilter filter)
    {
        if (filter.IsDeleted)
        {
            return query
                .OrderByDescending(n => n.DeletedAtUtc)
                .ThenByDescending(n => n.UpdatedAtUtc)
                .ThenByDescending(n => n.Id);
        }

        var prioritizePinned = filter.IsArchived == false;
        if (prioritizePinned)
        {
            var pinnedFirst = query.OrderByDescending(n => n.IsPinned);
            return filter.SortBy switch
            {
                NoteSortBy.CreatedAtDesc => pinnedFirst.ThenByDescending(n => n.CreatedAtUtc).ThenByDescending(n => n.Id),
                NoteSortBy.PriorityDesc => pinnedFirst.ThenByDescending(n => n.Priority).ThenByDescending(n => n.UpdatedAtUtc).ThenByDescending(n => n.Id),
                NoteSortBy.TitleAsc => pinnedFirst.ThenBy(n => n.Title).ThenByDescending(n => n.Id),
                _ => pinnedFirst.ThenByDescending(n => n.UpdatedAtUtc).ThenByDescending(n => n.Id)
            };
        }

        return filter.SortBy switch
        {
            NoteSortBy.CreatedAtDesc => query.OrderByDescending(n => n.CreatedAtUtc).ThenByDescending(n => n.Id),
            NoteSortBy.PriorityDesc => query.OrderByDescending(n => n.Priority).ThenByDescending(n => n.UpdatedAtUtc).ThenByDescending(n => n.Id),
            NoteSortBy.TitleAsc => query.OrderBy(n => n.Title).ThenByDescending(n => n.Id),
            _ => query.OrderByDescending(n => n.UpdatedAtUtc).ThenByDescending(n => n.Id)
        };
    }
}
