using Vanderkeep.Dominio.Comum;
using Vanderkeep.Dominio.Enums;
using Vanderkeep.Dominio.Excecoes;

namespace Vanderkeep.Dominio.Entidades;

public sealed class Note : BaseEntity
{
    public const int MaxTitleLength = 150;
    public const int MaxDescriptionLength = 5000;

    private readonly List<NoteCategory> _noteCategories = [];

    private Note()
    {
    }

    public Note(
        Guid userId,
        string title,
        string? description,
        NotePriority priority,
        DateTime utcNow)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("Note owner identifier cannot be empty.");
        }

        UserId = userId;
        ApplyDetails(title, description, priority);
        IsPinned = false;
        IsArchived = false;
        IsDeleted = false;
        DeletedAtUtc = null;
        SetCreatedAudit(utcNow);
    }

    public Guid UserId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public NotePriority Priority { get; private set; } = NotePriority.Medium;

    public bool IsPinned { get; private set; }

    public bool IsArchived { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTime? DeletedAtUtc { get; private set; }

    public IReadOnlyCollection<NoteCategory> NoteCategories => _noteCategories.AsReadOnly();

    public void UpdateDetails(string title, string? description, NotePriority priority, DateTime utcNow)
    {
        EnsureNotDeleted("Cannot edit a note that is in the trash.");
        ApplyDetails(title, description, priority);
        SetUpdatedAudit(utcNow);
    }

    public void UpdateCategories(IEnumerable<Guid> categoryIds, DateTime utcNow)
    {
        EnsureNotDeleted("Cannot update categories of a note that is in the trash.");
        ArgumentNullException.ThrowIfNull(categoryIds);

        var desiredIds = new HashSet<Guid>();
        foreach (var categoryId in categoryIds)
        {
            if (categoryId == Guid.Empty)
            {
                throw new DomainException("Category identifier cannot be empty.");
            }

            desiredIds.Add(categoryId);
        }

        _noteCategories.RemoveAll(nc => !desiredIds.Contains(nc.CategoryId));

        foreach (var categoryId in desiredIds)
        {
            if (_noteCategories.All(nc => nc.CategoryId != categoryId))
            {
                _noteCategories.Add(new NoteCategory(Id, categoryId));
            }
        }

        SetUpdatedAudit(utcNow);
    }

    public void Pin(DateTime utcNow)
    {
        EnsureNotDeleted("Cannot pin a note that is in the trash.");

        if (IsArchived)
        {
            throw new DomainException("Cannot pin an archived note.");
        }

        if (IsPinned)
        {
            return;
        }

        IsPinned = true;
        SetUpdatedAudit(utcNow);
    }

    public void Unpin(DateTime utcNow)
    {
        EnsureNotDeleted("Cannot unpin a note that is in the trash.");

        if (!IsPinned)
        {
            return;
        }

        IsPinned = false;
        SetUpdatedAudit(utcNow);
    }

    public void Archive(DateTime utcNow)
    {
        EnsureNotDeleted("Cannot archive a note that is in the trash.");

        if (IsArchived && !IsPinned)
        {
            return;
        }

        IsArchived = true;
        IsPinned = false;
        SetUpdatedAudit(utcNow);
    }

    public void Unarchive(DateTime utcNow)
    {
        EnsureNotDeleted("Cannot unarchive a note that is in the trash.");

        if (!IsArchived)
        {
            return;
        }

        IsArchived = false;
        SetUpdatedAudit(utcNow);
    }

    public void MoveToTrash(DateTime utcNow)
    {
        if (IsDeleted)
        {
            throw new DomainException("Note is already in the trash.");
        }

        IsDeleted = true;
        DeletedAtUtc = utcNow;
        IsPinned = false;
        IsArchived = false;
        SetUpdatedAudit(utcNow);
    }

    public void Restore(DateTime utcNow)
    {
        if (!IsDeleted)
        {
            throw new DomainException("Note is not in the trash.");
        }

        IsDeleted = false;
        DeletedAtUtc = null;
        IsArchived = false;
        IsPinned = false;
        SetUpdatedAudit(utcNow);
    }

    private void EnsureNotDeleted(string message)
    {
        if (IsDeleted)
        {
            throw new DomainException(message);
        }
    }

    private void ApplyDetails(string title, string? description, NotePriority priority)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Note title is required.");
        }

        var trimmedTitle = title.Trim();
        if (trimmedTitle.Length > MaxTitleLength)
        {
            throw new DomainException($"Note title cannot exceed {MaxTitleLength} characters.");
        }

        string? normalizedDescription = null;
        if (!string.IsNullOrWhiteSpace(description))
        {
            normalizedDescription = description.Trim();
            if (normalizedDescription.Length > MaxDescriptionLength)
            {
                throw new DomainException($"Note description cannot exceed {MaxDescriptionLength} characters.");
            }
        }

        if (!Enum.IsDefined(priority))
        {
            throw new DomainException("Invalid note priority.");
        }

        Title = trimmedTitle;
        Description = normalizedDescription;
        Priority = priority;
    }
}
