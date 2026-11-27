using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VKeeper.Dominio.Entidades;
using VKeeper.Dominio.Enums;
using VKeeper.Infraestrutura.Identity;
using VKeeper.Infraestrutura.Persistencia;
using VKeeper.TestesIntegracao.Infraestrutura;

namespace VKeeper.TestesIntegracao.Web;

[Collection(IntegrationTestCollection.Name)]
public sealed class CategoriesIntegrationTests
{
    private readonly VKeeperWebApplicationFactory _factory;

    public CategoriesIntegrationTests(VKeeperWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/Categories")]
    [InlineData("/Categories/Create")]
    public async Task GetProtectedCategoryRoutes_WhenAnonymous_ShouldRedirectToLogin(string url)
    {
        using var client = CreateClientNoRedirect();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task CategoryCrudFlow_ShouldCreateListAlphabeticallyEditAndDelete()
    {
        using var client = CreateClientNoRedirect();
        var (userId, _) = await RegisterAndSignInAsync(client);

        // Create two categories out of alphabetical order.
        var createZ = await CreateCategoryAsync(client, "Zeta");
        var createA = await CreateCategoryAsync(client, "Alfa");
        Assert.Equal(HttpStatusCode.Redirect, createZ.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, createA.StatusCode);

        var indexHtml = await client.GetStringAsync("/Categories");
        var indexAlfa = indexHtml.IndexOf("Alfa", StringComparison.Ordinal);
        var indexZeta = indexHtml.IndexOf("Zeta", StringComparison.Ordinal);
        Assert.True(indexAlfa >= 0 && indexZeta > indexAlfa, "Categories should be listed in alphabetical order.");

        Guid alfaId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VKeeperDbContext>();
            alfaId = await db.Categories
                .Where(c => c.UserId == userId && c.Title == "Alfa")
                .Select(c => c.Id)
                .SingleAsync();
        }

        // Rename Alfa -> Beta (and also verify case-only rename on Beta -> BETA).
        var editResponse = await EditCategoryAsync(client, alfaId, "Beta");
        Assert.Equal(HttpStatusCode.Redirect, editResponse.StatusCode);

        var caseRenameResponse = await EditCategoryAsync(client, alfaId, "BETA");
        Assert.Equal(HttpStatusCode.Redirect, caseRenameResponse.StatusCode);

        var afterEditHtml = await client.GetStringAsync("/Categories");
        Assert.Contains("BETA", afterEditHtml);
        Assert.DoesNotContain(">Alfa<", afterEditHtml);

        // Delete BETA.
        var deleteResponse = await DeleteCategoryAsync(client, alfaId);
        Assert.Equal(HttpStatusCode.Redirect, deleteResponse.StatusCode);

        var afterDeleteHtml = await client.GetStringAsync("/Categories");
        Assert.DoesNotContain("BETA", afterDeleteHtml);
        Assert.Contains("Zeta", afterDeleteHtml);
    }

    [Fact]
    public async Task CategoryUniqueness_ShouldRejectDuplicateForSameUserAndAllowSameTitleForDifferentUsers()
    {
        using var clientA = CreateClientNoRedirect();
        await RegisterAndSignInAsync(clientA);

        var firstCreate = await CreateCategoryAsync(clientA, "Estudos");
        Assert.Equal(HttpStatusCode.Redirect, firstCreate.StatusCode);

        var duplicateCreate = await CreateCategoryAsync(clientA, "  ESTUDOS  ");
        var duplicateHtml = await duplicateCreate.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, duplicateCreate.StatusCode);
        Assert.Contains("Já existe uma categoria cadastrada com este título.", duplicateHtml);

        // A different user must be able to create a category with the same title (RN05).
        using var clientB = CreateClientNoRedirect();
        await RegisterAndSignInAsync(clientB);

        var userBCreate = await CreateCategoryAsync(clientB, "Estudos");
        Assert.Equal(HttpStatusCode.Redirect, userBCreate.StatusCode);
    }

    [Fact]
    public async Task CategoryNoteCountAndDelete_ShouldIgnoreTrashNotesInCountAndPreserveAllNotesOnDelete()
    {
        using var client = CreateClientNoRedirect();
        var (userId, _) = await RegisterAndSignInAsync(client);

        var createResponse = await CreateCategoryAsync(client, "Projetos");
        Assert.Equal(HttpStatusCode.Redirect, createResponse.StatusCode);

        Guid categoryId;
        Guid activeNoteId;
        Guid archivedNoteId;
        Guid deletedNoteId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VKeeperDbContext>();
            categoryId = await db.Categories
                .Where(c => c.UserId == userId && c.Title == "Projetos")
                .Select(c => c.Id)
                .SingleAsync();

            var now = DateTime.UtcNow;

            var activeNote = new Note(userId, "Nota Ativa", null, NotePriority.Medium, now);
            activeNote.UpdateCategories(new[] { categoryId }, now);

            var archivedNote = new Note(userId, "Nota Arquivada", null, NotePriority.Low, now);
            archivedNote.UpdateCategories(new[] { categoryId }, now);
            archivedNote.Archive(now);

            var deletedNote = new Note(userId, "Nota na Lixeira", null, NotePriority.High, now);
            deletedNote.UpdateCategories(new[] { categoryId }, now);
            deletedNote.MoveToTrash(now);

            db.Notes.AddRange(activeNote, archivedNote, deletedNote);
            await db.SaveChangesAsync();

            activeNoteId = activeNote.Id;
            archivedNoteId = archivedNote.Id;
            deletedNoteId = deletedNote.Id;
        }

        // Index must display count = 2 (active + archived; ignoring note in trash per RN08).
        var indexHtml = await client.GetStringAsync("/Categories");
        Assert.Contains("2 notas", indexHtml);

        // Delete confirmation page must warn that notes will remain intact.
        var deleteGetHtml = await client.GetStringAsync($"/Categories/Delete/{categoryId}");
        Assert.Contains("2 notas", deleteGetHtml);
        Assert.Contains("suas notas permanecerão intactas", deleteGetHtml);

        // Delete category via POST.
        var deletePostResponse = await DeleteCategoryAsync(client, categoryId);
        Assert.Equal(HttpStatusCode.Redirect, deletePostResponse.StatusCode);

        // Verify in SQL Server: category and N:N links are gone, all 3 notes remain intact (RN06).
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VKeeperDbContext>();
            Assert.False(await db.Categories.AnyAsync(c => c.Id == categoryId));
            Assert.False(await db.NoteCategories.AnyAsync(nc => nc.CategoryId == categoryId));

            var remainingNotes = await db.Notes
                .Where(n => n.Id == activeNoteId || n.Id == archivedNoteId || n.Id == deletedNoteId)
                .ToListAsync();
            Assert.Equal(3, remainingNotes.Count);
        }
    }

    [Fact]
    public async Task AntiIdor_UserBCannotViewEditOrDeleteCategoryOfUserA()
    {
        using var clientA = CreateClientNoRedirect();
        var (userAId, _) = await RegisterAndSignInAsync(clientA);
        await CreateCategoryAsync(clientA, "Segredo de A");

        Guid categoryAId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VKeeperDbContext>();
            categoryAId = await db.Categories
                .Where(c => c.UserId == userAId)
                .Select(c => c.Id)
                .SingleAsync();
        }

        using var clientB = CreateClientNoRedirect();
        await RegisterAndSignInAsync(clientB);

        // Obtain a valid antiforgery token from B's own Create page to test POST IDOR directly.
        var createPageHtml = await clientB.GetStringAsync("/Categories/Create");
        var tokenB = AntiForgeryHelper.ExtractRequestVerificationToken(createPageHtml);

        var getEdit = await clientB.GetAsync($"/Categories/Edit/{categoryAId}");
        Assert.Equal(HttpStatusCode.NotFound, getEdit.StatusCode);

        using var editForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = tokenB,
            ["Id"] = categoryAId.ToString(),
            ["Title"] = "Hackeada por B"
        });
        var postEdit = await clientB.PostAsync($"/Categories/Edit/{categoryAId}", editForm);
        Assert.Equal(HttpStatusCode.NotFound, postEdit.StatusCode);

        var getDelete = await clientB.GetAsync($"/Categories/Delete/{categoryAId}");
        Assert.Equal(HttpStatusCode.NotFound, getDelete.StatusCode);

        using var deleteForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = tokenB
        });
        var postDelete = await clientB.PostAsync($"/Categories/Delete/{categoryAId}", deleteForm);
        Assert.Equal(HttpStatusCode.NotFound, postDelete.StatusCode);

        // Confirm category A is completely untouched.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VKeeperDbContext>();
            var categoryA = await db.Categories.SingleOrDefaultAsync(c => c.Id == categoryAId);
            Assert.NotNull(categoryA);
            Assert.Equal("Segredo de A", categoryA.Title);
        }
    }

    [Fact]
    public async Task PostCreateWithoutAntiForgeryToken_ShouldReturnBadRequest()
    {
        using var client = CreateClientNoRedirect();
        await RegisterAndSignInAsync(client);

        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Title"] = "Sem CSRF"
        });

        var response = await client.PostAsync("/Categories/Create", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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

    private async Task<(Guid UserId, string Email)> RegisterAndSignInAsync(HttpClient client)
    {
        var email = $"cat-{Guid.NewGuid():N}@vkeeper.local";
        var getHtml = await client.GetStringAsync("/Account/Register");
        var token = AntiForgeryHelper.ExtractRequestVerificationToken(getHtml);

        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["DisplayName"] = "Usuario Categoria",
            ["Email"] = email,
            ["Password"] = "SenhaForte1",
            ["ConfirmPassword"] = "SenhaForte1"
        });

        var response = await client.PostAsync("/Account/Register", form);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        Assert.NotNull(user);

        return (user.Id, email);
    }

    private static async Task<HttpResponseMessage> CreateCategoryAsync(HttpClient client, string title)
    {
        var getHtml = await client.GetStringAsync("/Categories/Create");
        var token = AntiForgeryHelper.ExtractRequestVerificationToken(getHtml);

        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Title"] = title
        });

        return await client.PostAsync("/Categories/Create", form);
    }

    private static async Task<HttpResponseMessage> EditCategoryAsync(HttpClient client, Guid id, string newTitle)
    {
        var getHtml = await client.GetStringAsync($"/Categories/Edit/{id}");
        var token = AntiForgeryHelper.ExtractRequestVerificationToken(getHtml);

        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Id"] = id.ToString(),
            ["Title"] = newTitle
        });

        return await client.PostAsync($"/Categories/Edit/{id}", form);
    }

    private static async Task<HttpResponseMessage> DeleteCategoryAsync(HttpClient client, Guid id)
    {
        var getHtml = await client.GetStringAsync($"/Categories/Delete/{id}");
        var token = AntiForgeryHelper.ExtractRequestVerificationToken(getHtml);

        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token
        });

        return await client.PostAsync($"/Categories/Delete/{id}", form);
    }
}
