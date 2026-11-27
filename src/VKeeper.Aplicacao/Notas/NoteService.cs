using FluentValidation;
using VKeeper.Aplicacao.Comum;
using VKeeper.Aplicacao.Notas.AtualizarNota;
using VKeeper.Aplicacao.Notas.CriarNota;
using VKeeper.Aplicacao.Notas.ListarNotas;
using VKeeper.Aplicacao.Notas.Lixeira;
using VKeeper.Aplicacao.Notas.ObterNota;
using VKeeper.Dominio.Entidades;
using VKeeper.Dominio.Interfaces;

namespace VKeeper.Aplicacao.Notas;

public sealed class NoteService : INoteService
{
    private const int DescriptionPreviewMaxLength = 180;
    private const string UnauthenticatedMessage = "Usuário não autenticado.";
    private const string NoteNotFoundMessage = "Nota não encontrada.";
    private const string InvalidCategoriesMessage = "Uma ou mais categorias selecionadas não existem ou não pertencem à sua conta.";

    private readonly INoteRepository _noteRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IValidator<CreateNoteRequest> _createValidator;
    private readonly IValidator<UpdateNoteRequest> _updateValidator;

    public NoteService(
        INoteRepository noteRepository,
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IValidator<CreateNoteRequest> createValidator,
        IValidator<UpdateNoteRequest> updateValidator)
    {
        _noteRepository = noteRepository;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<Result<PagedResult<NoteListItemDto>>> ListAsync(
        ListNotesQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!TryGetUserId(out var userId))
        {
            return Result<PagedResult<NoteListItemDto>>.Forbidden(UnauthenticatedMessage);
        }

        var pageNumber = Math.Max(1, query.PageNumber);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var filter = new NoteQueryFilter(
            UserId: userId,
            IsDeleted: false,
            IsArchived: query.IsArchived,
            SearchTerm: query.SearchTerm,
            CategoryId: query.CategoryId,
            Priority: query.Priority,
            SortBy: query.SortBy,
            PageNumber: pageNumber,
            PageSize: pageSize);

        var (notes, totalCount) = await _noteRepository.ListPagedAsync(filter, cancellationToken);
        var effectivePage = ResolveEffectivePage(pageNumber, pageSize, totalCount);

        var items = notes
            .Select(n => new NoteListItemDto(
                n.Id,
                n.Title,
                BuildPreview(n.Description),
                n.Priority,
                n.IsPinned,
                n.IsArchived,
                n.CreatedAtUtc,
                n.UpdatedAtUtc,
                MapCategories(n)))
            .ToArray();

        return Result<PagedResult<NoteListItemDto>>.Success(
            new PagedResult<NoteListItemDto>(items, effectivePage, pageSize, totalCount));
    }

    public async Task<Result<PagedResult<TrashNoteItemDto>>> ListTrashAsync(
        ListTrashQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!TryGetUserId(out var userId))
        {
            return Result<PagedResult<TrashNoteItemDto>>.Forbidden(UnauthenticatedMessage);
        }

        var pageNumber = Math.Max(1, query.PageNumber);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var filter = new NoteQueryFilter(
            UserId: userId,
            IsDeleted: true,
            IsArchived: null,
            PageNumber: pageNumber,
            PageSize: pageSize);

        var (notes, totalCount) = await _noteRepository.ListPagedAsync(filter, cancellationToken);
        var effectivePage = ResolveEffectivePage(pageNumber, pageSize, totalCount);

        var items = notes
            .Select(MapTrashItem)
            .ToArray();

        return Result<PagedResult<TrashNoteItemDto>>.Success(
            new PagedResult<TrashNoteItemDto>(items, effectivePage, pageSize, totalCount));
    }

    public async Task<Result<NoteDetailsDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId))
        {
            return Result<NoteDetailsDto>.Forbidden(UnauthenticatedMessage);
        }

        if (id == Guid.Empty)
        {
            return Result<NoteDetailsDto>.NotFound(NoteNotFoundMessage);
        }

        var note = await _noteRepository.GetByIdAsync(
            id,
            userId,
            includeCategories: true,
            includeDeleted: false,
            asNoTracking: true,
            cancellationToken: cancellationToken);

        if (note is null)
        {
            return Result<NoteDetailsDto>.NotFound(NoteNotFoundMessage);
        }

        return Result<NoteDetailsDto>.Success(new NoteDetailsDto(
            note.Id,
            note.Title,
            note.Description,
            note.Priority,
            note.IsPinned,
            note.IsArchived,
            note.CreatedAtUtc,
            note.UpdatedAtUtc,
            MapCategories(note)));
    }

    public async Task<Result<TrashNoteItemDto>> GetTrashItemByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId))
        {
            return Result<TrashNoteItemDto>.Forbidden(UnauthenticatedMessage);
        }

        if (id == Guid.Empty)
        {
            return Result<TrashNoteItemDto>.NotFound(NoteNotFoundMessage);
        }

        var note = await _noteRepository.GetByIdAsync(
            id,
            userId,
            includeCategories: true,
            includeDeleted: true,
            asNoTracking: true,
            cancellationToken: cancellationToken);

        if (note is null || !note.IsDeleted)
        {
            return Result<TrashNoteItemDto>.NotFound(NoteNotFoundMessage);
        }

        return Result<TrashNoteItemDto>.Success(MapTrashItem(note));
    }

    public async Task<Result<Guid>> CreateAsync(CreateNoteRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!TryGetUserId(out var userId))
        {
            return Result<Guid>.Forbidden(UnauthenticatedMessage);
        }

        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<Guid>.ValidationFailure(validation.Errors.Select(e => e.ErrorMessage));
        }

        var categoryIds = NormalizeCategoryIds(request.CategoryIds);
        if (!await ValidateOwnedCategoriesAsync(categoryIds, userId, cancellationToken))
        {
            return Result<Guid>.ValidationFailure(new[] { InvalidCategoriesMessage });
        }

        var now = _dateTimeProvider.UtcNow;
        var note = new Note(userId, request.Title, request.Description, request.Priority, now);

        if (categoryIds.Length > 0)
        {
            note.UpdateCategories(categoryIds, now);
        }

        await _noteRepository.AddAsync(note, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(note.Id);
    }

    public async Task<Result> UpdateAsync(UpdateNoteRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!TryGetUserId(out var userId))
        {
            return Result.Forbidden(UnauthenticatedMessage);
        }

        if (request.Id == Guid.Empty)
        {
            return Result.NotFound(NoteNotFoundMessage);
        }

        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result.ValidationFailure(validation.Errors.Select(e => e.ErrorMessage));
        }

        var note = await _noteRepository.GetByIdAsync(
            request.Id,
            userId,
            includeCategories: true,
            includeDeleted: false,
            asNoTracking: false,
            cancellationToken: cancellationToken);

        if (note is null)
        {
            return Result.NotFound(NoteNotFoundMessage);
        }

        var categoryIds = NormalizeCategoryIds(request.CategoryIds);
        if (!await ValidateOwnedCategoriesAsync(categoryIds, userId, cancellationToken))
        {
            return Result.ValidationFailure(new[] { InvalidCategoriesMessage });
        }

        var now = _dateTimeProvider.UtcNow;
        note.UpdateDetails(request.Title, request.Description, request.Priority, now);
        note.UpdateCategories(categoryIds, now);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> PinAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId))
        {
            return Result.Forbidden(UnauthenticatedMessage);
        }

        if (id == Guid.Empty)
        {
            return Result.NotFound(NoteNotFoundMessage);
        }

        var note = await _noteRepository.GetByIdAsync(
            id,
            userId,
            includeCategories: false,
            includeDeleted: false,
            asNoTracking: false,
            cancellationToken: cancellationToken);

        if (note is null)
        {
            return Result.NotFound(NoteNotFoundMessage);
        }

        if (note.IsArchived)
        {
            return Result.ValidationFailure(new[] { "Não é possível fixar uma nota arquivada." });
        }

        note.Pin(_dateTimeProvider.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> UnpinAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId))
        {
            return Result.Forbidden(UnauthenticatedMessage);
        }

        if (id == Guid.Empty)
        {
            return Result.NotFound(NoteNotFoundMessage);
        }

        var note = await _noteRepository.GetByIdAsync(
            id,
            userId,
            includeCategories: false,
            includeDeleted: false,
            asNoTracking: false,
            cancellationToken: cancellationToken);

        if (note is null)
        {
            return Result.NotFound(NoteNotFoundMessage);
        }

        note.Unpin(_dateTimeProvider.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId))
        {
            return Result.Forbidden(UnauthenticatedMessage);
        }

        if (id == Guid.Empty)
        {
            return Result.NotFound(NoteNotFoundMessage);
        }

        var note = await _noteRepository.GetByIdAsync(
            id,
            userId,
            includeCategories: false,
            includeDeleted: false,
            asNoTracking: false,
            cancellationToken: cancellationToken);

        if (note is null)
        {
            return Result.NotFound(NoteNotFoundMessage);
        }

        note.Archive(_dateTimeProvider.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> UnarchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId))
        {
            return Result.Forbidden(UnauthenticatedMessage);
        }

        if (id == Guid.Empty)
        {
            return Result.NotFound(NoteNotFoundMessage);
        }

        var note = await _noteRepository.GetByIdAsync(
            id,
            userId,
            includeCategories: false,
            includeDeleted: false,
            asNoTracking: false,
            cancellationToken: cancellationToken);

        if (note is null)
        {
            return Result.NotFound(NoteNotFoundMessage);
        }

        note.Unarchive(_dateTimeProvider.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> MoveToTrashAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId))
        {
            return Result.Forbidden(UnauthenticatedMessage);
        }

        if (id == Guid.Empty)
        {
            return Result.NotFound(NoteNotFoundMessage);
        }

        var note = await _noteRepository.GetByIdAsync(
            id,
            userId,
            includeCategories: false,
            includeDeleted: false,
            asNoTracking: false,
            cancellationToken: cancellationToken);

        if (note is null)
        {
            return Result.NotFound(NoteNotFoundMessage);
        }

        note.MoveToTrash(_dateTimeProvider.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId))
        {
            return Result.Forbidden(UnauthenticatedMessage);
        }

        if (id == Guid.Empty)
        {
            return Result.NotFound(NoteNotFoundMessage);
        }

        var note = await _noteRepository.GetByIdAsync(
            id,
            userId,
            includeCategories: false,
            includeDeleted: true,
            asNoTracking: false,
            cancellationToken: cancellationToken);

        if (note is null || !note.IsDeleted)
        {
            return Result.NotFound(NoteNotFoundMessage);
        }

        note.Restore(_dateTimeProvider.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> DeletePermanentlyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId))
        {
            return Result.Forbidden(UnauthenticatedMessage);
        }

        if (id == Guid.Empty)
        {
            return Result.NotFound(NoteNotFoundMessage);
        }

        var note = await _noteRepository.GetByIdAsync(
            id,
            userId,
            includeCategories: false,
            includeDeleted: true,
            asNoTracking: false,
            cancellationToken: cancellationToken);

        if (note is null || !note.IsDeleted)
        {
            return Result.NotFound(NoteNotFoundMessage);
        }

        _noteRepository.Remove(note);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<int>> EmptyTrashAsync(CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId))
        {
            return Result<int>.Forbidden(UnauthenticatedMessage);
        }

        var deletedNotes = await _noteRepository.ListDeletedByUserAsync(userId, cancellationToken);
        if (deletedNotes.Count == 0)
        {
            return Result<int>.Success(0);
        }

        _noteRepository.RemoveRange(deletedNotes);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<int>.Success(deletedNotes.Count);
    }

    private async Task<bool> ValidateOwnedCategoriesAsync(
        Guid[] categoryIds,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (categoryIds.Length == 0)
        {
            return true;
        }

        var ownedCategories = await _categoryRepository.GetByIdsAsync(categoryIds, userId, cancellationToken);
        return ownedCategories.Count == categoryIds.Length;
    }

    private static Guid[] NormalizeCategoryIds(IReadOnlyCollection<Guid>? categoryIds)
    {
        if (categoryIds is null || categoryIds.Count == 0)
        {
            return Array.Empty<Guid>();
        }

        return categoryIds.Distinct().ToArray();
    }

    private static IReadOnlyList<NoteCategoryBadgeDto> MapCategories(Note note)
    {
        return note.NoteCategories
            .Where(nc => nc.Category is not null)
            .OrderBy(nc => nc.Category.Title)
            .Select(nc => new NoteCategoryBadgeDto(nc.CategoryId, nc.Category.Title))
            .ToArray();
    }

    private static TrashNoteItemDto MapTrashItem(Note note)
    {
        return new TrashNoteItemDto(
            note.Id,
            note.Title,
            BuildPreview(note.Description),
            note.Priority,
            note.DeletedAtUtc ?? note.UpdatedAtUtc,
            note.UpdatedAtUtc,
            MapCategories(note));
    }

    private static int ResolveEffectivePage(int requestedPage, int pageSize, int totalCount)
    {
        if (totalCount <= 0)
        {
            return 1;
        }

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return Math.Min(requestedPage, totalPages);
    }

    private static string? BuildPreview(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        return description.Length <= DescriptionPreviewMaxLength
            ? description
            : string.Concat(description.AsSpan(0, DescriptionPreviewMaxLength).TrimEnd(), "...");
    }

    private bool TryGetUserId(out Guid userId)
    {
        if (_currentUserService.IsAuthenticated &&
            _currentUserService.UserId is Guid authenticatedUserId &&
            authenticatedUserId != Guid.Empty)
        {
            userId = authenticatedUserId;
            return true;
        }

        userId = Guid.Empty;
        return false;
    }
}
