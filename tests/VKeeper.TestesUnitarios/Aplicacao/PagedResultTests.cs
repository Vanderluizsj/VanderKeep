using VKeeper.Aplicacao.Comum;

namespace VKeeper.TestesUnitarios.Aplicacao;

public sealed class PagedResultTests
{
    [Fact]
    public void Constructor_WithValidPaging_ShouldCalculateTotalPagesAndNavigationFlags()
    {
        var result = new PagedResult<string>(new[] { "A", "B" }, pageNumber: 2, pageSize: 12, totalCount: 25);

        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
    }

    [Fact]
    public void Constructor_WithZeroTotalCount_ShouldHaveZeroPagesAndNoNavigation()
    {
        var result = new PagedResult<string>(Array.Empty<string>(), pageNumber: 1, pageSize: 12, totalCount: 0);

        Assert.Equal(0, result.TotalPages);
        Assert.False(result.HasPreviousPage);
        Assert.False(result.HasNextPage);
    }

    [Theory]
    [InlineData(0, 12, 10)]
    [InlineData(1, 0, 10)]
    [InlineData(1, 12, -1)]
    public void Constructor_WithInvalidArguments_ShouldThrowArgumentOutOfRangeException(int pageNumber, int pageSize, int totalCount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PagedResult<string>(Array.Empty<string>(), pageNumber, pageSize, totalCount));
    }
}
