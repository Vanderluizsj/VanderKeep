using VKeeper.Aplicacao.Categorias.AtualizarCategoria;
using VKeeper.Aplicacao.Categorias.CriarCategoria;
using VKeeper.Aplicacao.Categorias.ListarCategorias;
using VKeeper.Aplicacao.Comum;

namespace VKeeper.Aplicacao.Categorias;

public interface ICategoryService
{
    Task<Result<IReadOnlyList<CategoryListItemDto>>> ListAsync(CancellationToken cancellationToken = default);

    Task<Result<CategoryListItemDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<Guid>> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);

    Task<Result> UpdateAsync(UpdateCategoryRequest request, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
