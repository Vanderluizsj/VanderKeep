using VKeeper.Aplicacao.Notas.ListarNotas;
using VKeeper.Dominio.Enums;

namespace VKeeper.Aplicacao.Notas.ObterNota;

public sealed record NoteDetailsDto(
    Guid Id,
    string Title,
    string? Description,
    NotePriority Priority,
    bool IsPinned,
    bool IsArchived,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<NoteCategoryBadgeDto> Categories);
