using VKeeper.Dominio.Enums;

namespace VKeeper.Aplicacao.Notas.CriarNota;

public sealed record CreateNoteRequest(
    string Title,
    string? Description,
    NotePriority Priority,
    IReadOnlyCollection<Guid>? CategoryIds);
