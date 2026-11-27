namespace VKeeper.Aplicacao.Categorias.ListarCategorias;

public sealed record CategoryListItemDto(
    Guid Id,
    string Title,
    int NoteCount,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
