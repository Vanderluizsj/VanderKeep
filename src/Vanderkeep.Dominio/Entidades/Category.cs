using Vanderkeep.Dominio.Comum;
using Vanderkeep.Dominio.Excecoes;

namespace Vanderkeep.Dominio.Entidades;

public sealed class Category : BaseEntity
{
    public const int MaxTitleLength = 50;

    private readonly List<NoteCategory> _noteCategories = [];

    private Category()
    {
    }

    public Category(Guid userId, string title, DateTime utcNow)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("Category owner identifier cannot be empty.");
        }

        UserId = userId;
        ApplyTitle(title);
        SetCreatedAudit(utcNow);
    }

    public Guid UserId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string NormalizedTitle { get; private set; } = string.Empty;

    public IReadOnlyCollection<NoteCategory> NoteCategories => _noteCategories.AsReadOnly();

    public void Rename(string title, DateTime utcNow)
    {
        ApplyTitle(title);
        SetUpdatedAudit(utcNow);
    }

    public static string NormalizeTitle(string title)
    {
        ArgumentNullException.ThrowIfNull(title);
        return title.Trim().ToUpperInvariant();
    }

    private void ApplyTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Category title is required.");
        }

        var trimmed = title.Trim();
        if (trimmed.Length > MaxTitleLength)
        {
            throw new DomainException($"Category title cannot exceed {MaxTitleLength} characters.");
        }

        Title = trimmed;
        NormalizedTitle = NormalizeTitle(trimmed);
    }
}
