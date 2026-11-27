using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using VKeeper.Web.Servicos;

namespace VKeeper.TestesIntegracao.Web;

public sealed class CurrentUserServiceTests
{
    [Fact]
    public void UserIdAndIsAuthenticated_WhenHttpContextIsNull_ShouldReturnNullAndFalse()
    {
        var accessor = new HttpContextAccessor { HttpContext = null };
        var service = new CurrentUserService(accessor);

        Assert.Null(service.UserId);
        Assert.False(service.IsAuthenticated);
    }

    [Fact]
    public void UserIdAndIsAuthenticated_WhenPrincipalIsNotAuthenticated_ShouldReturnNullAndFalseEvenWithClaim()
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, Guid.CreateVersion7().ToString()) });
        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
        var service = new CurrentUserService(accessor);

        Assert.Null(service.UserId);
        Assert.False(service.IsAuthenticated);
    }

    [Fact]
    public void UserIdAndIsAuthenticated_WhenAuthenticatedWithInvalidGuidClaim_ShouldReturnNullAndFalse()
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, "not-a-valid-guid") },
            authenticationType: "Cookies");
        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
        var service = new CurrentUserService(accessor);

        Assert.Null(service.UserId);
        Assert.False(service.IsAuthenticated);
    }

    [Fact]
    public void UserIdAndIsAuthenticated_WhenAuthenticatedWithValidGuidClaim_ShouldReturnUserIdAndTrue()
    {
        var expectedId = Guid.CreateVersion7();
        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, expectedId.ToString()) },
            authenticationType: "Cookies");
        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
        var service = new CurrentUserService(accessor);

        Assert.Equal(expectedId, service.UserId);
        Assert.True(service.IsAuthenticated);
    }
}
