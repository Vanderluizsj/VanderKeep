using VKeeper.Dominio.Enums;

namespace VKeeper.Aplicacao.Notas.ListarNotas;

public sealed record ListNotesQuery(
    bool IsArchived = false,
    string? SearchTerm = null,
    Guid? CategoryId = null,
    NotePriority? Priority = null,
    NoteSortBy SortBy = NoteSortBy.UpdatedAtDesc,
    int PageNumber = 1,
    int PageSize = 12);
