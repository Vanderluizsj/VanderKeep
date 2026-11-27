using FluentValidation;
using VKeeper.Aplicacao.Categorias.AtualizarCategoria;
using VKeeper.Aplicacao.Categorias.CriarCategoria;
using VKeeper.Aplicacao.Categorias.ListarCategorias;
using VKeeper.Aplicacao.Comum;
using VKeeper.Dominio.Entidades;
using VKeeper.Dominio.Excecoes;
using VKeeper.Dominio.Interfaces;

namespace VKeeper.Aplicacao.Categorias;

public sealed class CategoryService : ICategoryService
{
    private const string UnauthenticatedMessage = "Usuário não autenticado.";
    private const string CategoryNotFoundMessage = "Categoria não encontrada.";
    private const string DuplicateCategoryMessage = "Já existe uma categoria cadastrada com este título.";

    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IValidator<CreateCategoryRequest> _createValidator;
    private readonly IValidator<UpdateCategoryRequest> _updateValidator;

    public CategoryService(
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IValidator<CreateCategoryRequest> createValidator,
        IValidator<UpdateCategoryRequest> updateValidator)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<Result<IReadOnlyList<CategoryListItemDto>>> ListAsync(CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId))
        {
            return Result<IReadOnlyList<CategoryListItemDto>>.Forbidden(UnauthenticatedMessage);
        }

        var summaries = await _categoryRepository.ListWithNoteCountByUserAsync(userId, cancellationToken);
        var items = summaries
            .Select(s => new CategoryListItemDto(s.Id, s.Title, s.NoteCount, s.CreatedAtUtc, s.UpdatedAtUtc))
            .ToArray();

        return Result<IReadOnlyList<CategoryListItemDto>>.Success(items);
    }

    public async Task<Result<CategoryListItemDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId))
        {
            return Result<CategoryListItemDto>.Forbidden(UnauthenticatedMessage);
        }

        if (id == Guid.Empty)
        {
            return Result<CategoryListItemDto>.NotFound(CategoryNotFoundMessage);
        }

        var summary = await _categoryRepository.GetSummaryByIdAsync(id, userId, cancellationToken);
        if (summary is null)
        {
            return Result<CategoryListItemDto>.NotFound(CategoryNotFoundMessage);
        }

        return Result<CategoryListItemDto>.Success(
            new CategoryListItemDto(summary.Id, summary.Title, summary.NoteCount, summary.CreatedAtUtc, summary.UpdatedAtUtc));
    }

    public async Task<Result<Guid>> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
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

        var normalizedTitle = Category.NormalizeTitle(request.Title);
        var exists = await _categoryRepository.ExistsByNormalizedTitleAsync(userId, normalizedTitle, cancellationToken: cancellationToken);
        if (exists)
        {
            return Result<Guid>.Conflict(DuplicateCategoryMessage);
        }

        var category = new Category(userId, request.Title, _dateTimeProvider.UtcNow);
        await _categoryRepository.AddAsync(category, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            return Result<Guid>.Conflict(DuplicateCategoryMessage);
        }

        return Result<Guid>.Success(category.Id);
    }

    public async Task<Result> UpdateAsync(UpdateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!TryGetUserId(out var userId))
        {
            return Result.Forbidden(UnauthenticatedMessage);
        }

        if (request.Id == Guid.Empty)
        {
            return Result.NotFound(CategoryNotFoundMessage);
        }

        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result.ValidationFailure(validation.Errors.Select(e => e.ErrorMessage));
        }

        var category = await _categoryRepository.GetByIdAsync(request.Id, userId, cancellationToken);
        if (category is null)
        {
            return Result.NotFound(CategoryNotFoundMessage);
        }

        var normalizedTitle = Category.NormalizeTitle(request.Title);
        var duplicateExists = await _categoryRepository.ExistsByNormalizedTitleAsync(
            userId,
            normalizedTitle,
            excludeCategoryId: category.Id,
            cancellationToken: cancellationToken);

        if (duplicateExists)
        {
            return Result.Conflict(DuplicateCategoryMessage);
        }

        category.Rename(request.Title, _dateTimeProvider.UtcNow);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            return Result.Conflict(DuplicateCategoryMessage);
        }

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId))
        {
            return Result.Forbidden(UnauthenticatedMessage);
        }

        if (id == Guid.Empty)
        {
            return Result.NotFound(CategoryNotFoundMessage);
        }

        var category = await _categoryRepository.GetByIdAsync(id, userId, cancellationToken);
        if (category is null)
        {
            return Result.NotFound(CategoryNotFoundMessage);
        }

        _categoryRepository.Remove(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private bool TryGetUserId(out Guid userId)
    {
        if (_currentUserService.IsAuthenticated && _currentUserService.UserId is Guid authenticatedUserId && authenticatedUserId != Guid.Empty)
        {
            userId = authenticatedUserId;
            return true;
        }

        userId = Guid.Empty;
        return false;
    }
}
