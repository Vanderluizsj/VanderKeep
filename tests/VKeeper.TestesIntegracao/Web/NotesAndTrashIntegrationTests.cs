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
public sealed class NotesAndTrashIntegrationTests
{
    private readonly VKeeperWebApplicationFactory _factory;

    public NotesAndTrashIntegrationTests(VKeeperWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/Notes")]
    [InlineData("/Notes/Archived")]
    [InlineData("/Notes/Create")]
    [InlineData("/Trash")]
    public async Task ProtectedNotesAndTrashRoutes_WhenAnonymous_ShouldRedirectToLogin(string url)
    {
        using var client = CreateClientNoRedirect();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task GetHome_WhenAuthenticated_ShouldRedirectDirectlyToNotes()
    {
        using var client = CreateClientNoRedirect();
        await RegisterAndSignInAsync(client);

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Notes", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task NoteCrudAndManyToManyCategoriesFlow_ShouldPersistCorrectlyInSqlServer()
    {
        using var client = CreateClientNoRedirect();
        var userId = await RegisterAndSignInAsync(client);

        var catWorkId = await SeedCategoryAsync(userId, "Trabalho");
        var catStudyId = await SeedCategoryAsync(userId, "Estudos");
        var catHomeId = await SeedCategoryAsync(userId, "Casa");

        // 1. Create note without categories and without description.
        var createSimple = await CreateNoteAsync(client, "Nota Rápida", null, NotePriority.Low, Array.Empty<Guid>());
        Assert.Equal(HttpStatusCode.Redirect, createSimple.StatusCode);

        // 2. Create note with multiple categories.
        var createMulti = await CreateNoteAsync(
            client,
            "Planejar Sprint",
            "Revisar backlog e arquitetura",
            NotePriority.High,
            new[] { catWorkId, catStudyId });
        Assert.Equal(HttpStatusCode.Redirect, createMulti.StatusCode);

        Guid sprintNoteId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VKeeperDbContext>();
            var note = await db.Notes
                .Include(n => n.NoteCategories)
                .SingleAsync(n => n.UserId == userId && n.Title == "Planejar Sprint");
            sprintNoteId = note.Id;
            Assert.Equal(2, note.NoteCategories.Count);
        }

        // 3. View details.
        var detailsHtml = await client.GetStringAsync($"/Notes/Details/{sprintNoteId}");
        Assert.Contains("Planejar Sprint", detailsHtml);
        Assert.Contains("Revisar backlog e arquitetura", detailsHtml);
        Assert.Contains("Trabalho", detailsHtml);
        Assert.Contains("Estudos", detailsHtml);

        // 4. Edit note updating title, priority and replacing categories (Estudos + Casa).
        var editResponse = await EditNoteAsync(
            client,
            sprintNoteId,
            "Sprint Atualizada",
            "Escopo refinado",
            NotePriority.Medium,
            new[] { catStudyId, catHomeId });
        Assert.Equal(HttpStatusCode.Redirect, editResponse.StatusCode);
        Assert.Equal($"/Notes/Details/{sprintNoteId}", editResponse.Headers.Location?.OriginalString);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VKeeperDbContext>();
            var updated = await db.Notes
                .Include(n => n.NoteCategories)
                .SingleAsync(n => n.Id == sprintNoteId);
            Assert.Equal("Sprint Atualizada", updated.Title);
            Assert.Equal(NotePriority.Medium, updated.Priority);
            Assert.Equal(2, updated.NoteCategories.Count);
            Assert.Contains(updated.NoteCategories, nc => nc.CategoryId == catStudyId);
            Assert.Contains(updated.NoteCategories, nc => nc.CategoryId == catHomeId);
            Assert.DoesNotContain(updated.NoteCategories, nc => nc.CategoryId == catWorkId);
        }
    }

    [Fact]
    public async Task CreateOrEditNote_WithAnotherUsersCategory_ShouldBeRejectedByRN03()
    {
        using var clientA = CreateClientNoRedirect();
        var userAId = await RegisterAndSignInAsync(clientA);
        var categoryOfA = await SeedCategoryAsync(userAId, "Categoria de A");

        using var clientB = CreateClientNoRedirect();
        var userBId = await RegisterAndSignInAsync(clientB);
        var categoryOfB = await SeedCategoryAsync(userBId, "Categoria de B");

        // 1. Create with foreign category must fail.
        var createResponse = await CreateNoteAsync(
            clientA,
            "Tentativa Indevida",
            "Desc",
            NotePriority.Medium,
            new[] { categoryOfB });
        var html = await createResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        Assert.Contains("não pertencem à sua conta", html);

        // 2. Edit an existing note attempting to attach foreign category must also fail and preserve original links.
        var validCreate = await CreateNoteAsync(clientA, "Nota Legítima", "Desc", NotePriority.Low, new[] { categoryOfA });
        Assert.Equal(HttpStatusCode.Redirect, validCreate.StatusCode);

        Guid noteAId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VKeeperDbContext>();
            Assert.False(await db.Notes.AnyAsync(n => n.UserId == userAId && n.Title == "Tentativa Indevida"));
            noteAId = await db.Notes.Where(n => n.UserId == userAId && n.Title == "Nota Legítima").Select(n => n.Id).SingleAsync();
        }

        var editForeignResponse = await EditNoteAsync(
            clientA,
            noteAId,
            "Nota Editada Indevidamente",
            "Desc",
            NotePriority.High,
            new[] { categoryOfB });
        var editForeignHtml = await editForeignResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, editForeignResponse.StatusCode);
        Assert.Contains("não pertencem à sua conta", editForeignHtml);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VKeeperDbContext>();
            var noteA = await db.Notes.Include(n => n.NoteCategories).SingleAsync(n => n.Id == noteAId);
            Assert.Equal("Nota Legítima", noteA.Title);
            Assert.Single(noteA.NoteCategories);
            Assert.Equal(categoryOfA, noteA.NoteCategories.Single().CategoryId);
        }
    }

    [Fact]
    public async Task PinAndArchiveFlow_ShouldOrderPinnedFirstClearPinOnArchiveAndRestoreOnUnarchive()
    {
        using var client = CreateClientNoRedirect();
        var userId = await RegisterAndSignInAsync(client);

        Guid olderNoteId;
        Guid newerNoteId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VKeeperDbContext>();
            var t1 = DateTime.UtcNow.AddHours(-2);
            var t2 = DateTime.UtcNow.AddHours(-1);

            var older = new Note(userId, "Nota Antiga Fixada", null, NotePriority.Low, t1);
            var newer = new Note(userId, "Nota Mais Recente", null, NotePriority.High, t2);
            db.Notes.AddRange(older, newer);
            await db.SaveChangesAsync();

            olderNoteId = older.Id;
            newerNoteId = newer.Id;
        }

        // Pin the older note -> must appear BEFORE newer note on /Notes.
        var token = await GetNotesAntiForgeryTokenAsync(client);
        var pinResponse = await PostActionAsync(client, $"/Notes/Pin/{olderNoteId}", token);
        Assert.Equal(HttpStatusCode.Redirect, pinResponse.StatusCode);

        var activeHtml = await client.GetStringAsync("/Notes");
        var posOlder = activeHtml.IndexOf("Nota Antiga Fixada", StringComparison.Ordinal);
        var posNewer = activeHtml.IndexOf("Nota Mais Recente", StringComparison.Ordinal);
        Assert.True(posOlder >= 0 && posNewer > posOlder, "Pinned note must appear before non-pinned newer notes.");

        // Archive the pinned note -> leaves /Notes, appears in /Notes/Archived with IsPinned == false.
        var archiveResponse = await PostActionAsync(client, $"/Notes/Archive/{olderNoteId}", token);
        Assert.Equal(HttpStatusCode.Redirect, archiveResponse.StatusCode);

        var activeAfterArchive = await client.GetStringAsync("/Notes");
        Assert.DoesNotContain("Nota Antiga Fixada", activeAfterArchive);

        var archivedHtml = await client.GetStringAsync("/Notes/Archived");
        Assert.Contains("Nota Antiga Fixada", archivedHtml);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VKeeperDbContext>();
            var archivedNote = await db.Notes.SingleAsync(n => n.Id == olderNoteId);
            Assert.True(archivedNote.IsArchived);
            Assert.False(archivedNote.IsPinned);
        }

        // Pinning an archived note must not pin it.
        var pinArchivedResponse = await PostActionAsync(client, $"/Notes/Pin/{olderNoteId}", token);
        Assert.Equal(HttpStatusCode.Redirect, pinArchivedResponse.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VKeeperDbContext>();
            var stillArchived = await db.Notes.SingleAsync(n => n.Id == olderNoteId);
            Assert.False(stillArchived.IsPinned);
        }

        // Unarchive -> returns to active notes.
        var unarchiveResponse = await PostActionAsync(client, $"/Notes/Unarchive/{olderNoteId}", token);
        Assert.Equal(HttpStatusCode.Redirect, unarchiveResponse.StatusCode);

        var activeAfterUnarchive = await client.GetStringAsync("/Notes");
        Assert.Contains("Nota Antiga Fixada", activeAfterUnarchive);
    }

    [Fact]
    public async Task SearchCombinedFiltersSortingAndPagination_ShouldWorkAccuratelyInSqlServer()
    {
        using var client = CreateClientNoRedirect();
        var userId = await RegisterAndSignInAsync(client);
        var catBackend = await SeedCategoryAsync(userId, "Backend");
        var catOther = await SeedCategoryAsync(userId, "Outros");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VKeeperDbContext>();
            var baseTime = DateTime.UtcNow.AddDays(-1);

            for (var i = 1; i <= 14; i++)
            {
                var note = new Note(
                    userId,
                    $"Anotação {i:D2}",
                    i == 5 ? "Conteúdo especial sobre EF Core" : "Texto padrão",
                    i == 5 ? NotePriority.High : NotePriority.Medium,
                    baseTime.AddMinutes(i));
                note.UpdateCategories(new[] { i == 5 ? catBackend : catOther }, baseTime.AddMinutes(i));
                db.Notes.Add(note);
            }

            await db.SaveChangesAsync();
        }

        // Page 1 must show 12 of 14, Page 2 must show 2 of 14.
        var page1Html = await client.GetStringAsync("/Notes?page=1");
        Assert.Contains("Exibindo <strong>12</strong> de <strong>14</strong>", page1Html);
        Assert.Contains("Página 1 de 2", page1Html);

        // Page 2 must show 2 of 14.
        var page2Html = await client.GetStringAsync("/Notes?page=2");
        Assert.Contains("Exibindo <strong>2</strong> de <strong>14</strong>", page2Html);
        Assert.Contains("Página 2 de 2", page2Html);

        // Page beyond TotalPages must clamp to the last valid page instead of showing a false empty state (M1).
        var page99Html = await client.GetStringAsync("/Notes?page=99");
        Assert.Contains("Exibindo <strong>2</strong> de <strong>14</strong>", page99Html);
        Assert.Contains("Página 2 de 2", page99Html);
        Assert.DoesNotContain("Você ainda não possui notas", page99Html);

        // Combined filter: searchTerm + categoryId + priority.
        var filteredHtml = await client.GetStringAsync(
            $"/Notes?searchTerm=EF%20Core&categoryId={catBackend}&priority=High&sortBy=TitleAsc");
        Assert.Contains("Anotação 05", filteredHtml);
        Assert.Contains("Exibindo <strong>1</strong> de <strong>1</strong>", filteredHtml);
        Assert.DoesNotContain("Anotação 01", filteredHtml);
    }

    [Fact]
    public async Task TrashLifecycle_ShouldMoveRestoreDeletePermanentlyAndEmptyOnlyOwnTrash()
    {
        using var clientA = CreateClientNoRedirect();
        var userAId = await RegisterAndSignInAsync(clientA);
        var catAId = await SeedCategoryAsync(userAId, "Importante");

        using var clientB = CreateClientNoRedirect();
        var userBId = await RegisterAndSignInAsync(clientB);

        Guid noteToRestoreId;
        Guid noteToDeletePermanentlyId;
        Guid noteToEmptyTrashId;
        Guid activeNoteAId;
        Guid trashNoteBId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VKeeperDbContext>();
            var now = DateTime.UtcNow;

            var n1 = new Note(userAId, "Nota Restaurável", null, NotePriority.High, now);
            n1.UpdateCategories(new[] { catAId }, now);
            var n2 = new Note(userAId, "Nota Exclusão Definitiva", null, NotePriority.Low, now);
            var n3 = new Note(userAId, "Nota Esvaziar Lixeira", null, NotePriority.Medium, now);
            var n4 = new Note(userAId, "Nota Ativa Preservada", null, NotePriority.Medium, now);

            var nB = new Note(userBId, "Lixeira de B", null, NotePriority.Medium, now);
            nB.MoveToTrash(now);

            db.Notes.AddRange(n1, n2, n3, n4, nB);
            await db.SaveChangesAsync();

            noteToRestoreId = n1.Id;
            noteToDeletePermanentlyId = n2.Id;
            noteToEmptyTrashId = n3.Id;
            activeNoteAId = n4.Id;
            trashNoteBId = nB.Id;
        }

        var tokenA = await GetNotesAntiForgeryTokenAsync(clientA);

        // Move 3 notes of User A to trash.
        Assert.Equal(HttpStatusCode.Redirect, (await PostActionAsync(clientA, $"/Notes/MoveToTrash/{noteToRestoreId}", tokenA)).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await PostActionAsync(clientA, $"/Notes/MoveToTrash/{noteToDeletePermanentlyId}", tokenA)).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await PostActionAsync(clientA, $"/Notes/MoveToTrash/{noteToEmptyTrashId}", tokenA)).StatusCode);

        // Trashed notes must not be editable directly (404).
        Assert.Equal(HttpStatusCode.NotFound, (await clientA.GetAsync($"/Notes/Edit/{noteToRestoreId}")).StatusCode);

        // Trash page displays A's trashed notes, never B's.
        var trashHtml = await clientA.GetStringAsync("/Trash");
        Assert.Contains("Nota Restaurável", trashHtml);
        Assert.Contains("Nota Exclusão Definitiva", trashHtml);
        Assert.DoesNotContain("Lixeira de B", trashHtml);

        // Restore note 1 -> back to /Notes with category preserved.
        var restoreResponse = await PostActionAsync(clientA, $"/Trash/Restore/{noteToRestoreId}", tokenA);
        Assert.Equal(HttpStatusCode.Redirect, restoreResponse.StatusCode);

        var restoredDetailsHtml = await clientA.GetStringAsync($"/Notes/Details/{noteToRestoreId}");
        Assert.Contains("Nota Restaurável", restoredDetailsHtml);
        Assert.Contains("Importante", restoredDetailsHtml);

        // Permanently delete note 2.
        var deleteConfirmHtml = await clientA.GetStringAsync($"/Trash/Delete/{noteToDeletePermanentlyId}");
        Assert.Contains("Esta ação é irreversível", deleteConfirmHtml);
        var deletePermResponse = await PostActionAsync(clientA, $"/Trash/Delete/{noteToDeletePermanentlyId}", tokenA);
        Assert.Equal(HttpStatusCode.Redirect, deletePermResponse.StatusCode);

        // Empty remaining trash of User A (removes note 3, keeps active note 4 & restored note 1 & User B's trash).
        var emptyConfirmHtml = await clientA.GetStringAsync("/Trash/Empty");
        Assert.Contains("Esvaziar lixeira", emptyConfirmHtml);
        var emptyResponse = await PostActionAsync(clientA, "/Trash/Empty", tokenA);
        Assert.Equal(HttpStatusCode.Redirect, emptyResponse.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VKeeperDbContext>();
            Assert.True(await db.Notes.AnyAsync(n => n.Id == noteToRestoreId && !n.IsDeleted));
            Assert.True(await db.Notes.AnyAsync(n => n.Id == activeNoteAId && !n.IsDeleted));
            Assert.False(await db.Notes.AnyAsync(n => n.Id == noteToDeletePermanentlyId));
            Assert.False(await db.Notes.AnyAsync(n => n.Id == noteToEmptyTrashId));
            Assert.True(await db.Notes.AnyAsync(n => n.Id == trashNoteBId && n.IsDeleted));
        }
    }

    [Fact]
    public async Task ArchivingLastItemOnPageTwo_ShouldClampToPageOneWithoutFalseEmptyState()
    {
        using var client = CreateClientNoRedirect();
        var userId = await RegisterAndSignInAsync(client);

        Guid oldestNoteId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VKeeperDbContext>();
            var baseTime = DateTime.UtcNow.AddHours(-2);
            var notes = new List<Note>();
            for (var i = 1; i <= 13; i++)
            {
                notes.Add(new Note(userId, $"Nota Paginada {i:D2}", null, NotePriority.Medium, baseTime.AddMinutes(i)));
            }

            db.Notes.AddRange(notes);
            await db.SaveChangesAsync();
            oldestNoteId = notes[0].Id; // Only item on page 2 when sorted by UpdatedAtDesc.
        }

        var token = await GetNotesAntiForgeryTokenAsync(client);
        using var archiveForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["returnUrl"] = "/Notes?page=2"
        });

        var archiveResponse = await client.PostAsync($"/Notes/Archive/{oldestNoteId}", archiveForm);
        Assert.Equal(HttpStatusCode.Redirect, archiveResponse.StatusCode);
        Assert.Equal("/Notes?page=2", archiveResponse.Headers.Location?.OriginalString);

        // Following redirect to /Notes?page=2 must clamp to page 1 showing the 12 remaining notes.
        var clampedHtml = await client.GetStringAsync("/Notes?page=2");
        Assert.Contains("Exibindo <strong>12</strong> de <strong>12</strong>", clampedHtml);
        Assert.DoesNotContain("Você ainda não possui notas", clampedHtml);

        // Out-of-range page on Trash with items must also clamp cleanly.
        var trashResponse = await PostActionAsync(client, $"/Notes/MoveToTrash/{oldestNoteId}", token);
        Assert.Equal(HttpStatusCode.Redirect, trashResponse.StatusCode);
        var clampedTrashHtml = await client.GetStringAsync("/Trash?page=99");
        Assert.Contains("Nota Paginada 01", clampedTrashHtml);
        Assert.DoesNotContain("Sua lixeira está vazia", clampedTrashHtml);
    }

    [Theory]
    [InlineData("https://evil.example.com/phish")]
    [InlineData("//evil.example.com/phish")]
    public async Task NotesActions_WithUnsafeReturnUrl_ShouldIgnoreExternalUrlAndRedirectLocally(string unsafeReturnUrl)
    {
        using var client = CreateClientNoRedirect();
        var userId = await RegisterAndSignInAsync(client);

        Guid noteId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VKeeperDbContext>();
            var note = new Note(userId, "Nota ReturnUrl", null, NotePriority.Medium, DateTime.UtcNow);
            db.Notes.Add(note);
            await db.SaveChangesAsync();
            noteId = note.Id;
        }

        var token = await GetNotesAntiForgeryTokenAsync(client);
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["returnUrl"] = unsafeReturnUrl
        });

        var pinResponse = await client.PostAsync($"/Notes/Pin/{noteId}", form);
        Assert.Equal(HttpStatusCode.Redirect, pinResponse.StatusCode);
        Assert.Equal("/Notes", pinResponse.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task AntiIdor_UserBCannotReadEditModifyRestoreOrDeleteUserANotes()
    {
        using var clientA = CreateClientNoRedirect();
        var userAId = await RegisterAndSignInAsync(clientA);

        Guid activeNoteAId;
        Guid pinnedNoteAId;
        Guid archivedNoteAId;
        Guid trashedNoteAId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VKeeperDbContext>();
            var now = DateTime.UtcNow;
            var activeA = new Note(userAId, "Segredo Ativo A", "Privado", NotePriority.High, now);
            var pinnedA = new Note(userAId, "Segredo Fixado A", "Privado", NotePriority.Medium, now);
            pinnedA.Pin(now);
            var archivedA = new Note(userAId, "Segredo Arquivado A", "Privado", NotePriority.Low, now);
            archivedA.Archive(now);
            var trashedA = new Note(userAId, "Segredo Lixeira A", "Privado", NotePriority.Low, now);
            trashedA.MoveToTrash(now);

            db.Notes.AddRange(activeA, pinnedA, archivedA, trashedA);
            await db.SaveChangesAsync();

            activeNoteAId = activeA.Id;
            pinnedNoteAId = pinnedA.Id;
            archivedNoteAId = archivedA.Id;
            trashedNoteAId = trashedA.Id;
        }

        using var clientB = CreateClientNoRedirect();
        await RegisterAndSignInAsync(clientB);
        var tokenB = await GetNotesAntiForgeryTokenAsync(clientB);

        Assert.Equal(HttpStatusCode.NotFound, (await clientB.GetAsync($"/Notes/Details/{activeNoteAId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await clientB.GetAsync($"/Notes/Edit/{activeNoteAId}")).StatusCode);

        using var postEditForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = tokenB,
            ["Id"] = activeNoteAId.ToString(),
            ["Title"] = "Hackeada por B",
            ["Description"] = "Alterada",
            ["Priority"] = NotePriority.Low.ToString()
        });
        Assert.Equal(HttpStatusCode.NotFound, (await clientB.PostAsync($"/Notes/Edit/{activeNoteAId}", postEditForm)).StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await PostActionAsync(clientB, $"/Notes/Pin/{activeNoteAId}", tokenB)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await PostActionAsync(clientB, $"/Notes/Unpin/{pinnedNoteAId}", tokenB)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await PostActionAsync(clientB, $"/Notes/Archive/{activeNoteAId}", tokenB)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await PostActionAsync(clientB, $"/Notes/Unarchive/{archivedNoteAId}", tokenB)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await PostActionAsync(clientB, $"/Notes/MoveToTrash/{activeNoteAId}", tokenB)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await PostActionAsync(clientB, $"/Trash/Restore/{trashedNoteAId}", tokenB)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await clientB.GetAsync($"/Trash/Delete/{trashedNoteAId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await PostActionAsync(clientB, $"/Trash/Delete/{trashedNoteAId}", tokenB)).StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VKeeperDbContext>();
            var activeA = await db.Notes.SingleAsync(n => n.Id == activeNoteAId);
            var pinnedA = await db.Notes.SingleAsync(n => n.Id == pinnedNoteAId);
            var archivedA = await db.Notes.SingleAsync(n => n.Id == archivedNoteAId);
            var trashedA = await db.Notes.SingleAsync(n => n.Id == trashedNoteAId);
            Assert.Equal("Segredo Ativo A", activeA.Title);
            Assert.False(activeA.IsPinned);
            Assert.False(activeA.IsArchived);
            Assert.False(activeA.IsDeleted);
            Assert.True(pinnedA.IsPinned);
            Assert.True(archivedA.IsArchived);
            Assert.True(trashedA.IsDeleted);
        }
    }

    [Fact]
    public async Task MutatingNoteAndTrashPostsWithoutAntiForgeryToken_ShouldReturnBadRequest()
    {
        using var client = CreateClientNoRedirect();
        await RegisterAndSignInAsync(client);

        using var createForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Title"] = "Sem CSRF",
            ["Priority"] = "Medium"
        });
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/Notes/Create", createForm)).StatusCode);

        using var emptyForm = new FormUrlEncodedContent(Array.Empty<KeyValuePair<string, string>>());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/Trash/Empty", emptyForm)).StatusCode);
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

    private async Task<Guid> RegisterAndSignInAsync(HttpClient client)
    {
        var email = $"notes-{Guid.NewGuid():N}@vkeeper.local";
        var getHtml = await client.GetStringAsync("/Account/Register");
        var token = AntiForgeryHelper.ExtractRequestVerificationToken(getHtml);

        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["DisplayName"] = "Usuario Notas",
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
        return user.Id;
    }

    private async Task<Guid> SeedCategoryAsync(Guid userId, string title)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VKeeperDbContext>();
        var category = new Category(userId, title, DateTime.UtcNow);
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return category.Id;
    }

    private static async Task<string> GetNotesAntiForgeryTokenAsync(HttpClient client)
    {
        var html = await client.GetStringAsync("/Notes/Create");
        return AntiForgeryHelper.ExtractRequestVerificationToken(html);
    }

    private static async Task<HttpResponseMessage> CreateNoteAsync(
        HttpClient client,
        string title,
        string? description,
        NotePriority priority,
        IEnumerable<Guid> categoryIds)
    {
        var token = await GetNotesAntiForgeryTokenAsync(client);
        var fields = new List<KeyValuePair<string, string>>
        {
            new("__RequestVerificationToken", token),
            new("Title", title),
            new("Description", description ?? string.Empty),
            new("Priority", priority.ToString())
        };

        foreach (var categoryId in categoryIds)
        {
            fields.Add(new("SelectedCategoryIds", categoryId.ToString()));
        }

        using var form = new FormUrlEncodedContent(fields);
        return await client.PostAsync("/Notes/Create", form);
    }

    private static async Task<HttpResponseMessage> EditNoteAsync(
        HttpClient client,
        Guid noteId,
        string title,
        string? description,
        NotePriority priority,
        IEnumerable<Guid> categoryIds)
    {
        var editHtml = await client.GetStringAsync($"/Notes/Edit/{noteId}");
        var token = AntiForgeryHelper.ExtractRequestVerificationToken(editHtml);
        var fields = new List<KeyValuePair<string, string>>
        {
            new("__RequestVerificationToken", token),
            new("Id", noteId.ToString()),
            new("Title", title),
            new("Description", description ?? string.Empty),
            new("Priority", priority.ToString())
        };

        foreach (var categoryId in categoryIds)
        {
            fields.Add(new("SelectedCategoryIds", categoryId.ToString()));
        }

        using var form = new FormUrlEncodedContent(fields);
        return await client.PostAsync($"/Notes/Edit/{noteId}", form);
    }

    private static async Task<HttpResponseMessage> PostActionAsync(HttpClient client, string url, string token)
    {
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token
        });

        return await client.PostAsync(url, form);
    }
}
