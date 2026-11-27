using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using VKeeper.Infraestrutura.Identity;
using VKeeper.TestesIntegracao.Infraestrutura;

namespace VKeeper.TestesIntegracao.Web;

[Collection(IntegrationTestCollection.Name)]
public sealed class AccountIntegrationTests
{
    private readonly VKeeperWebApplicationFactory _factory;

    public AccountIntegrationTests(VKeeperWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetChangePassword_WhenAnonymous_ShouldRedirectToLoginWithLocalReturnUrl()
    {
        using var client = CreateClientNoRedirect();

        var response = await client.GetAsync("/Account/ChangePassword");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Contains("/Account/Login", response.Headers.Location!.OriginalString);
        Assert.Contains("ReturnUrl=%2FAccount%2FChangePassword", response.Headers.Location.OriginalString);
    }

    [Fact]
    public async Task Register_WithValidData_ShouldCreateVersion7UserSignInAndAllowProtectedAccess()
    {
        using var client = CreateClientNoRedirect();
        var email = CreateUniqueEmail();
        var displayName = "Maria Silva";

        var registerResponse = await RegisterUserAsync(client, displayName, email, "SenhaForte1");

        Assert.Equal(HttpStatusCode.Redirect, registerResponse.StatusCode);
        Assert.Equal("/Notes", registerResponse.Headers.Location?.OriginalString);

        var notesResponse = await client.GetAsync("/Notes");
        var notesHtml = await notesResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, notesResponse.StatusCode);
        Assert.Contains(displayName, notesHtml);
        Assert.Contains("Alterar senha", notesHtml);

        var changePasswordResponse = await client.GetAsync("/Account/ChangePassword");
        Assert.Equal(HttpStatusCode.OK, changePasswordResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var createdUser = await userManager.FindByEmailAsync(email);
        Assert.NotNull(createdUser);
        Assert.Equal(7, createdUser.Id.Version);
        Assert.Equal(displayName, createdUser.DisplayName);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldReturnPortugueseValidationMessage()
    {
        var email = CreateUniqueEmail();
        using (var firstClient = CreateClientNoRedirect())
        {
            var firstResponse = await RegisterUserAsync(firstClient, "Primeiro", email, "SenhaForte1");
            Assert.Equal(HttpStatusCode.Redirect, firstResponse.StatusCode);
        }

        using var secondClient = CreateClientNoRedirect();
        var duplicateResponse = await RegisterUserAsync(secondClient, "Segundo", email, "SenhaForte1");
        var html = await duplicateResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, duplicateResponse.StatusCode);
        Assert.Contains("Já existe uma conta cadastrada com este e-mail.", html);
    }

    [Fact]
    public async Task Login_WithUnregisteredEmailOrWrongPassword_ShouldReturnSameGenericErrorMessage()
    {
        var existingEmail = CreateUniqueEmail();
        using (var seedClient = CreateClientNoRedirect())
        {
            await RegisterUserAsync(seedClient, "Usuario Teste", existingEmail, "SenhaForte1");
        }

        using var client1 = CreateClientNoRedirect();
        var unknownResponse = await LoginUserAsync(client1, CreateUniqueEmail(), "Qualquer1");
        var unknownHtml = await unknownResponse.Content.ReadAsStringAsync();

        using var client2 = CreateClientNoRedirect();
        var wrongPasswordResponse = await LoginUserAsync(client2, existingEmail, "SenhaErrada9");
        var wrongPasswordHtml = await wrongPasswordResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, unknownResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, wrongPasswordResponse.StatusCode);
        Assert.Contains("E-mail ou senha inválidos.", unknownHtml);
        Assert.Contains("E-mail ou senha inválidos.", wrongPasswordHtml);
    }

    [Fact]
    public async Task Register_WithWeakPassword_ShouldReturnPortuguesePolicyErrors()
    {
        using var client = CreateClientNoRedirect();
        var response = await RegisterUserAsync(client, "Usuario Fraco", CreateUniqueEmail(), "senhafraca");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("A senha deve conter pelo menos um número.", html);
        Assert.Contains("A senha deve conter pelo menos uma letra maiúscula.", html);
    }

    [Fact]
    public async Task Login_WithValidLocalReturnUrl_ShouldRedirectToRequestedLocalPath()
    {
        var email = CreateUniqueEmail();
        using (var seedClient = CreateClientNoRedirect())
        {
            await RegisterUserAsync(seedClient, "Usuario Local", email, "SenhaForte1");
        }

        using var loginClient = CreateClientNoRedirect();
        var response = await LoginUserAsync(loginClient, email, "SenhaForte1", returnUrl: "/Account/ChangePassword");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Account/ChangePassword", response.Headers.Location?.OriginalString);
    }

    [Theory]
    [InlineData("https://evil.example.com/phish")]
    [InlineData("//evil.example.com/phish")]
    public async Task Login_WithExternalOrProtocolRelativeReturnUrl_ShouldIgnoreAndRedirectLocally(string unsafeReturnUrl)
    {
        var email = CreateUniqueEmail();
        using (var seedClient = CreateClientNoRedirect())
        {
            await RegisterUserAsync(seedClient, "Usuario Seguro", email, "SenhaForte1");
        }

        using var loginClient = CreateClientNoRedirect();
        var response = await LoginUserAsync(loginClient, email, "SenhaForte1", returnUrl: unsafeReturnUrl);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Notes", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Login_ShouldEmitSecureHttpOnlyLaxAuthenticationCookie()
    {
        var email = CreateUniqueEmail();
        using (var seedClient = CreateClientNoRedirect())
        {
            await RegisterUserAsync(seedClient, "Usuario Cookie", email, "SenhaForte1");
        }

        using var loginClient = CreateClientNoRedirect();
        var response = await LoginUserAsync(loginClient, email, "SenhaForte1");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var setCookies));
        var authCookie = Assert.Single(setCookies, c => c.Contains(".AspNetCore.Identity.Application=", StringComparison.Ordinal));
        Assert.Contains("httponly", authCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", authCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", authCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_AfterFiveConsecutiveFailures_ShouldLockOutAccountAndRejectEvenCorrectPassword()
    {
        var email = CreateUniqueEmail();
        using (var seedClient = CreateClientNoRedirect())
        {
            await RegisterUserAsync(seedClient, "Usuario Lockout", email, "SenhaForte1");
        }

        using var client = CreateClientNoRedirect();
        HttpResponseMessage lastResponse = null!;
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            lastResponse = await LoginUserAsync(client, email, "SenhaErrada9");
        }

        var lockedHtml = await lastResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, lastResponse.StatusCode);
        Assert.Contains("Conta temporariamente bloqueada", lockedHtml);

        // Even with the right password, login must remain rejected while locked out.
        var correctPasswordWhileLocked = await LoginUserAsync(client, email, "SenhaForte1");
        var stillLockedHtml = await correctPasswordWhileLocked.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, correctPasswordWhileLocked.StatusCode);
        Assert.Contains("Conta temporariamente bloqueada", stillLockedHtml);
    }

    [Fact]
    public async Task Logout_WithoutAntiForgeryToken_ShouldReturnBadRequest()
    {
        using var client = CreateClientNoRedirect();
        await RegisterUserAsync(client, "Usuario CSRF", CreateUniqueEmail(), "SenhaForte1");

        using var emptyContent = new FormUrlEncodedContent(Array.Empty<KeyValuePair<string, string>>());
        var response = await client.PostAsync("/Account/Logout", emptyContent);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithAntiForgeryToken_ShouldInvalidateSession()
    {
        using var client = CreateClientNoRedirect();
        await RegisterUserAsync(client, "Usuario Logout", CreateUniqueEmail(), "SenhaForte1");

        var notesHtml = await client.GetStringAsync("/Notes");
        var token = AntiForgeryHelper.ExtractRequestVerificationToken(notesHtml);

        using var logoutForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token
        });

        var logoutResponse = await client.PostAsync("/Account/Logout", logoutForm);
        Assert.Equal(HttpStatusCode.Redirect, logoutResponse.StatusCode);

        var protectedResponse = await client.GetAsync("/Account/ChangePassword");
        Assert.Equal(HttpStatusCode.Redirect, protectedResponse.StatusCode);
        Assert.Contains("/Account/Login", protectedResponse.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task ChangePassword_ShouldValidateCurrentPasswordAndAllowLoginWithNewPassword()
    {
        var email = CreateUniqueEmail();
        using var client = CreateClientNoRedirect();
        await RegisterUserAsync(client, "Usuario Senha", email, "SenhaAntiga1");

        // 1. Wrong current password must fail.
        var failedChange = await ChangePasswordAsync(client, "SenhaIncorreta9", "SenhaNova2");
        var failedHtml = await failedChange.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, failedChange.StatusCode);
        Assert.Contains("A senha atual informada está incorreta.", failedHtml);

        // 2. Valid current password must succeed and preserve authenticated session.
        var successChange = await ChangePasswordAsync(client, "SenhaAntiga1", "SenhaNova2");
        Assert.Equal(HttpStatusCode.Redirect, successChange.StatusCode);
        Assert.Equal("/Account/ChangePassword", successChange.Headers.Location?.OriginalString);

        var afterChangePage = await client.GetAsync("/Account/ChangePassword");
        var afterChangeHtml = await afterChangePage.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, afterChangePage.StatusCode);
        Assert.Contains("Senha alterada com sucesso.", afterChangeHtml);

        // 3. New login must reject old password and accept new password.
        using var freshClient = CreateClientNoRedirect();
        var oldLogin = await LoginUserAsync(freshClient, email, "SenhaAntiga1");
        Assert.Equal(HttpStatusCode.OK, oldLogin.StatusCode);

        var newLogin = await LoginUserAsync(freshClient, email, "SenhaNova2");
        Assert.Equal(HttpStatusCode.Redirect, newLogin.StatusCode);
    }

    [Fact]
    public async Task AccessDenied_ShouldRedirectToForbiddenErrorPage()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = true
        });

        var response = await client.GetAsync("/Account/AccessDenied");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("Acesso negado", html);
    }

    private HttpClient CreateClientNoRedirect()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true
        });
    }

    private static string CreateUniqueEmail() => $"teste-{Guid.NewGuid():N}@vkeeper.local";

    private static async Task<HttpResponseMessage> RegisterUserAsync(
        HttpClient client,
        string displayName,
        string email,
        string password,
        string? returnUrl = null)
    {
        var getUrl = string.IsNullOrWhiteSpace(returnUrl)
            ? "/Account/Register"
            : $"/Account/Register?returnUrl={Uri.EscapeDataString(returnUrl)}";

        var getResponse = await client.GetAsync(getUrl);
        var getHtml = await getResponse.Content.ReadAsStringAsync();
        var token = AntiForgeryHelper.ExtractRequestVerificationToken(getHtml);

        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["DisplayName"] = displayName,
            ["Email"] = email,
            ["Password"] = password,
            ["ConfirmPassword"] = password,
            ["ReturnUrl"] = returnUrl ?? string.Empty
        });

        return await client.PostAsync("/Account/Register", form);
    }

    private static async Task<HttpResponseMessage> LoginUserAsync(
        HttpClient client,
        string email,
        string password,
        string? returnUrl = null)
    {
        var getUrl = string.IsNullOrWhiteSpace(returnUrl)
            ? "/Account/Login"
            : $"/Account/Login?returnUrl={Uri.EscapeDataString(returnUrl)}";

        var getResponse = await client.GetAsync(getUrl);
        var getHtml = await getResponse.Content.ReadAsStringAsync();
        var token = AntiForgeryHelper.ExtractRequestVerificationToken(getHtml);

        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Email"] = email,
            ["Password"] = password,
            ["RememberMe"] = "false",
            ["ReturnUrl"] = returnUrl ?? string.Empty
        });

        return await client.PostAsync("/Account/Login", form);
    }

    private static async Task<HttpResponseMessage> ChangePasswordAsync(
        HttpClient client,
        string currentPassword,
        string newPassword)
    {
        var getResponse = await client.GetAsync("/Account/ChangePassword");
        var getHtml = await getResponse.Content.ReadAsStringAsync();
        var token = AntiForgeryHelper.ExtractRequestVerificationToken(getHtml);

        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["CurrentPassword"] = currentPassword,
            ["NewPassword"] = newPassword,
            ["ConfirmNewPassword"] = newPassword
        });

        return await client.PostAsync("/Account/ChangePassword", form);
    }
}
