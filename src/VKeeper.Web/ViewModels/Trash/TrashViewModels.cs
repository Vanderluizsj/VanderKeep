using VKeeper.Aplicacao.Comum;
using VKeeper.Aplicacao.Notas.ListarNotas;
using VKeeper.Aplicacao.Notas.Lixeira;
using VKeeper.Dominio.Enums;

namespace VKeeper.Web.ViewModels.Trash;

public sealed class TrashListViewModel
{
    public PagedResult<TrashNoteItemDto> Notes { get; init; } =
        new(Array.Empty<TrashNoteItemDto>(), 1, 12, 0);
}

public sealed class DeleteNotePermanentlyViewModel
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public NotePriority Priority { get; init; }

    public DateTime DeletedAtUtc { get; init; }

    public IReadOnlyList<NoteCategoryBadgeDto> Categories { get; init; } =
        Array.Empty<NoteCategoryBadgeDto>();
}
