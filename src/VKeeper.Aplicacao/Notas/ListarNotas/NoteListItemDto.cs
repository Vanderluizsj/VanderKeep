using VKeeper.Dominio.Enums;

namespace VKeeper.Aplicacao.Notas.ListarNotas;

public sealed record NoteListItemDto(
    Guid Id,
    string Title,
    string? DescriptionPreview,
    NotePriority Priority,
    bool IsPinned,
    bool IsArchived,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<NoteCategoryBadgeDto> Categories);
