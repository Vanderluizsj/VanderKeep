using Vanderkeep.Dominio.Comum;
using Vanderkeep.Dominio.Excecoes;

namespace Vanderkeep.TestesUnitarios.Dominio;

public sealed class BaseEntityTests
{
    private sealed class SampleEntity : BaseEntity
    {
        public SampleEntity(DateTime createdAtUtc)
        {
            SetCreatedAudit(createdAtUtc);
        }

        public void Touch(DateTime updatedAtUtc)
        {
            SetUpdatedAudit(updatedAtUtc);
        }
    }

    [Fact]
    public void Constructor_WhenCreated_ShouldGenerateNonEmptyVersion7Guid()
    {
        var now = new DateTime(2026, 11, 26, 20, 0, 0, DateTimeKind.Utc);

        var entity = new SampleEntity(now);

        Assert.NotEqual(Guid.Empty, entity.Id);
        Assert.Equal(7, entity.Id.Version);
        Assert.Equal(now, entity.CreatedAtUtc);
        Assert.Equal(now, entity.UpdatedAtUtc);
    }

    [Fact]
    public void SetUpdatedAudit_WhenCalled_ShouldUpdateOnlyUpdatedAtUtc()
    {
        var created = new DateTime(2026, 11, 26, 20, 0, 0, DateTimeKind.Utc);
        var updated = created.AddMinutes(10);
        var entity = new SampleEntity(created);

        entity.Touch(updated);

        Assert.Equal(created, entity.CreatedAtUtc);
        Assert.Equal(updated, entity.UpdatedAtUtc);
    }

    [Fact]
    public void DomainException_WhenCreatedWithMessage_ShouldExposeMessage()
    {
        var exception = new DomainException("Invalid domain operation.");

        Assert.Equal("Invalid domain operation.", exception.Message);
    }
}
