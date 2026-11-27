using Vanderkeep.Dominio.Entidades;

namespace Vanderkeep.Dominio.Interfaces;

public interface INoteRepository
{
    Task<Note?> GetByIdAsync(
        Guid id,
        Guid userId,
        bool includeCategories = true,
        bool includeDeleted = false,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Note> Items, int TotalCount)> ListPagedAsync(
        NoteQueryFilter filter,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Note>> ListDeletedByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task AddAsync(Note note, CancellationToken cancellationToken = default);

    void Remove(Note note);

    void RemoveRange(IEnumerable<Note> notes);
}
