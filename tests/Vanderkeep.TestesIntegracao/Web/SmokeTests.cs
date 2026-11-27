using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Vanderkeep.Aplicacao.Comum;
using Vanderkeep.Dominio.Interfaces;
using Vanderkeep.Infraestrutura.Persistencia;
using Vanderkeep.TestesIntegracao.Infraestrutura;

namespace Vanderkeep.TestesIntegracao.Web;

public sealed class SmokeTests : IClassFixture<VanderkeepWebApplicationFactory>
{
    private readonly VanderkeepWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SmokeTests(VanderkeepWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHome_ShouldReturnSuccessAndBrandTitle()
    {
        var response = await _client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Vanderkeep", content);
    }

    [Fact]
    public async Task GetUnknownRoute_ShouldReturnNotFoundWithFriendlyPage()
    {
        var response = await _client.GetAsync("/rota-inexistente-para-teste");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("Página não encontrada", content);
    }

    [Fact]
    public async Task PostToGetOnlyEndpoint_ShouldReExecuteFriendlyErrorPageWithoutCsrfFailure()
    {
        using var payload = new FormUrlEncodedContent(Array.Empty<KeyValuePair<string, string>>());

        var response = await _client.PostAsync("/", payload);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.Contains("Erro 405", content);
        Assert.Contains("Não foi possível concluir a solicitação.", content);
    }

    [Fact]
    public async Task PostError404_ShouldRenderFriendlyNotFoundPageWithoutAntiforgeryFailure()
    {
        using var payload = new FormUrlEncodedContent(Array.Empty<KeyValuePair<string, string>>());

        var response = await _client.PostAsync("/Error/404", payload);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("Página não encontrada", content);
    }

    [Theory]
    [InlineData(200)]
    [InlineData(204)]
    [InlineData(304)]
    [InlineData(-1)]
    [InlineData(999)]
    public async Task GetErrorWithOutOfRangeStatusCode_ShouldNormalizeToNotFound(int statusCode)
    {
        var response = await _client.GetAsync($"/Error/{statusCode}");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("Página não encontrada", content);
    }

    [Fact]
    public async Task GetError403_ShouldReturnForbiddenWithFriendlyPage()
    {
        var response = await _client.GetAsync("/Error/403");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("Acesso negado", content);
    }

    [Fact]
    public async Task GetError_ShouldReturnInternalServerErrorWithoutLeakingStackTrace()
    {
        var response = await _client.GetAsync("/Error");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("Ocorreu um erro inesperado", content);
        Assert.DoesNotContain("StackTrace", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DependencyInjection_ShouldResolveCoreFoundationServices()
    {
        using var scope = _factory.Services.CreateScope();
        var provider = scope.ServiceProvider;

        Assert.NotNull(provider.GetRequiredService<VanderkeepDbContext>());
        Assert.NotNull(provider.GetRequiredService<IUnitOfWork>());
        Assert.NotNull(provider.GetRequiredService<INoteRepository>());
        Assert.NotNull(provider.GetRequiredService<ICategoryRepository>());
        Assert.NotNull(provider.GetRequiredService<IDateTimeProvider>());
        Assert.NotNull(provider.GetRequiredService<ICurrentUserService>());
    }
}
