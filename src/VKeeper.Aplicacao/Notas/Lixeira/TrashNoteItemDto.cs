using VKeeper.Aplicacao.Notas.ListarNotas;
using VKeeper.Dominio.Enums;

namespace VKeeper.Aplicacao.Notas.Lixeira;

public sealed record TrashNoteItemDto(
    Guid Id,
    string Title,
    string? DescriptionPreview,
    NotePriority Priority,
    DateTime DeletedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<NoteCategoryBadgeDto> Categories);
