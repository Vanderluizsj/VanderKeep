using Vanderkeep.Dominio.Entidades;
using Vanderkeep.Dominio.Excecoes;

namespace Vanderkeep.TestesUnitarios.Dominio;

public sealed class CategoryTests
{
    private static readonly DateTime CreatedAt = new(2026, 11, 26, 21, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Constructor_WithValidData_ShouldTrimAndNormalizeTitle()
    {
        var userId = Guid.CreateVersion7();

        var category = new Category(userId, "  Estudos .NET  ", CreatedAt);

        Assert.NotEqual(Guid.Empty, category.Id);
        Assert.Equal(7, category.Id.Version);
        Assert.Equal(userId, category.UserId);
        Assert.Equal("Estudos .NET", category.Title);
        Assert.Equal("ESTUDOS .NET", category.NormalizedTitle);
        Assert.Equal(CreatedAt, category.CreatedAtUtc);
        Assert.Equal(CreatedAt, category.UpdatedAtUtc);
        Assert.Empty(category.NoteCategories);
    }

    [Fact]
    public void Constructor_WithEmptyUserId_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => new Category(Guid.Empty, "Trabalho", CreatedAt));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankTitle_ShouldThrowDomainException(string invalidTitle)
    {
        Assert.Throws<DomainException>(() => new Category(Guid.CreateVersion7(), invalidTitle, CreatedAt));
    }

    [Fact]
    public void Constructor_WhenTitleExceedsMaxLength_ShouldThrowDomainException()
    {
        var longTitle = new string('a', Category.MaxTitleLength + 1);

        Assert.Throws<DomainException>(() => new Category(Guid.CreateVersion7(), longTitle, CreatedAt));
    }

    [Fact]
    public void Rename_WithValidTitle_ShouldUpdateTitleNormalizedTitleAndAuditTimestamp()
    {
        var category = new Category(Guid.CreateVersion7(), "Pessoal", CreatedAt);
        var updatedAt = CreatedAt.AddMinutes(15);

        category.Rename("  Finanças  ", updatedAt);

        Assert.Equal("Finanças", category.Title);
        Assert.Equal("FINANÇAS", category.NormalizedTitle);
        Assert.Equal(CreatedAt, category.CreatedAtUtc);
        Assert.Equal(updatedAt, category.UpdatedAtUtc);
    }

    [Fact]
    public void Constructor_WithExactMaxTitleLength_ShouldSucceed()
    {
        var maxTitle = new string('a', Category.MaxTitleLength);

        var category = new Category(Guid.CreateVersion7(), maxTitle, CreatedAt);

        Assert.Equal(Category.MaxTitleLength, category.Title.Length);
    }

    [Fact]
    public void NormalizeTitle_WithNull_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Category.NormalizeTitle(null!));
    }

    [Fact]
    public void NormalizeTitle_WithSpacesAndMixedCase_ShouldTrimAndUppercase()
    {
        Assert.Equal("ESTUDOS C#", Category.NormalizeTitle("  estudos c#  "));
    }

    [Fact]
    public void Rename_WithInvalidTitle_ShouldThrowDomainException()
    {
        var category = new Category(Guid.CreateVersion7(), "Pessoal", CreatedAt);

        Assert.Throws<DomainException>(() => category.Rename("   ", CreatedAt.AddMinutes(1)));
    }
}
