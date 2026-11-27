using VKeeper.Aplicacao.Comum;
using VKeeper.Aplicacao.Notas.AtualizarNota;
using VKeeper.Aplicacao.Notas.CriarNota;
using VKeeper.Aplicacao.Notas.ListarNotas;
using VKeeper.Aplicacao.Notas.Lixeira;
using VKeeper.Aplicacao.Notas.ObterNota;

namespace VKeeper.Aplicacao.Notas;

public interface INoteService
{
    Task<Result<PagedResult<NoteListItemDto>>> ListAsync(
        ListNotesQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<PagedResult<TrashNoteItemDto>>> ListTrashAsync(
        ListTrashQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<NoteDetailsDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<TrashNoteItemDto>> GetTrashItemByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<Guid>> CreateAsync(CreateNoteRequest request, CancellationToken cancellationToken = default);

    Task<Result> UpdateAsync(UpdateNoteRequest request, CancellationToken cancellationToken = default);

    Task<Result> PinAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result> UnpinAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result> ArchiveAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result> UnarchiveAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result> MoveToTrashAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result> RestoreAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result> DeletePermanentlyAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<int>> EmptyTrashAsync(CancellationToken cancellationToken = default);
}
