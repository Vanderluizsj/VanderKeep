using VKeeper.Dominio.Enums;

namespace VKeeper.Aplicacao.Notas.AtualizarNota;

public sealed record UpdateNoteRequest(
    Guid Id,
    string Title,
    string? Description,
    NotePriority Priority,
    IReadOnlyCollection<Guid>? CategoryIds);
