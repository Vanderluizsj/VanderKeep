using VKeeper.Dominio.Enums;

namespace VKeeper.Dominio.Interfaces;

public sealed record NoteQueryFilter(
    Guid UserId,
    bool IsDeleted = false,
    bool? IsArchived = false,
    string? SearchTerm = null,
    Guid? CategoryId = null,
    NotePriority? Priority = null,
    NoteSortBy SortBy = NoteSortBy.UpdatedAtDesc,
    int PageNumber = 1,
    int PageSize = 12);
