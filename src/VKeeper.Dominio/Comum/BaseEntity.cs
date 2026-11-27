namespace VKeeper.Dominio.Comum;

public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.CreateVersion7();

    public DateTime CreatedAtUtc { get; protected set; }

    public DateTime UpdatedAtUtc { get; protected set; }

    protected void SetCreatedAudit(DateTime utcNow)
    {
        CreatedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    protected void SetUpdatedAudit(DateTime utcNow)
    {
        UpdatedAtUtc = utcNow;
    }
}
