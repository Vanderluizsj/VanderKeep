using VKeeper.Aplicacao.Notas.ListarNotas;
using VKeeper.Dominio.Enums;

namespace VKeeper.Web.ViewModels.Notes;

public sealed class NoteDetailsViewModel
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string? Description { get; init; }

    public NotePriority Priority { get; init; }

    public bool IsPinned { get; init; }

    public bool IsArchived { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime UpdatedAtUtc { get; init; }

    public IReadOnlyList<NoteCategoryBadgeDto> Categories { get; init; } =
        Array.Empty<NoteCategoryBadgeDto>();
}
