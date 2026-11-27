namespace VKeeper.Dominio.Interfaces;

public sealed record CategorySummary(
    Guid Id,
    string Title,
    int NoteCount,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
