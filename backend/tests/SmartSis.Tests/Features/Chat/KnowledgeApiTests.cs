using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Chat.Enums;
using SmartSis.Api.Features.Chat.Models.Dtos;
using SmartSis.Api.Features.Chat.Models.Entities;
using SmartSis.Tests.Infrastructure;

namespace SmartSis.Tests.Features.Chat;

/// <summary>T61 / T62: admin knowledge base. The text is stored and sent to the AI service, which writes the chunks.</summary>
public sealed class KnowledgeApiTests(ChatTestApp app) : IClassFixture<ChatTestApp>
{
    private static MultipartFormDataContent Form(string title, string fileName, string content)
    {
        var form = new MultipartFormDataContent();
        form.Add(new StringContent(title), "title");
        var file = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
        file.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        form.Add(file, "file", fileName);
        return form;
    }

    [Fact]
    public async Task Upload_Text_SendsContentToIngest_AndIsReady()
    {
        app.Ai.Reset();
        var (admin, _) = await app.SignInAsync(SisApiFactory.AdminId);

        var res = await admin.PostAsync("/api/admin/knowledge", Form("Exam rules", "exam-rules.md", "# Exams\nMake-up exams need a medical note."));

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var doc = await res.ReadAsync<KnowledgeDocumentDto>();
        Assert.Equal(KnowledgeStatus.Ready, doc.Status);
        Assert.True(doc.CanReingest);
        Assert.Equal("Demo Admin", doc.UploadedByName);
        var ingest = Assert.Single(app.Ai.Ingests);
        Assert.Equal(doc.Id, ingest.DocumentId);
        Assert.Contains("medical note", ingest.Content);

        var list = await (await admin.GetAsync("/api/admin/knowledge")).ReadAsync<List<KnowledgeDocumentDto>>();
        Assert.Contains(list, d => d.Id == doc.Id && d.Title == "Exam rules");
    }

    [Fact]
    public async Task Upload_WrongType_Returns400()
    {
        var (admin, _) = await app.SignInAsync(SisApiFactory.AdminId);
        var res = await admin.PostAsync("/api/admin/knowledge", Form("Binary", "policy.pdf", "%PDF-1.7"));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Contains("file", await res.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Upload_MissingTitle_Returns400()
    {
        var (admin, _) = await app.SignInAsync(SisApiFactory.AdminId);
        var res = await admin.PostAsync("/api/admin/knowledge", Form(" ", "faq.txt", "Some text"));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Upload_AiDown_MarksFailed_ThenReingestMakesItReady()
    {
        app.Ai.Reset();
        var (admin, _) = await app.SignInAsync(SisApiFactory.AdminId);
        app.Ai.Down = true;
        var failed = await (await admin.PostAsync("/api/admin/knowledge", Form("Library FAQ", "library.txt", "The library opens at 8."))).ReadAsync<KnowledgeDocumentDto>();
        app.Ai.Down = false;
        Assert.Equal(KnowledgeStatus.Failed, failed.Status);

        var res = await admin.PostAsync($"/api/admin/knowledge/{failed.Id}/reingest", null);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal(KnowledgeStatus.Ready, (await res.ReadAsync<KnowledgeDocumentDto>()).Status);
        Assert.Contains("library opens", Assert.Single(app.Ai.Ingests).Content);
    }

    [Fact]
    public async Task Delete_RemovesDocumentChunksAndFile()
    {
        app.Ai.Reset();
        var (admin, _) = await app.SignInAsync(SisApiFactory.AdminId);
        var doc = await (await admin.PostAsync("/api/admin/knowledge", Form("Parking", "parking.txt", "Parking permits are free."))).ReadAsync<KnowledgeDocumentDto>();
        // What the AI service would have written.
        await app.Factory.WithDbAsync(async db =>
        {
            db.Add(new KnowledgeChunk { DocumentId = doc.Id, Content = "Parking permits are free.", Embedding = [0, 0, 128, 63] });
            await db.SaveChangesAsync();
        });

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/admin/knowledge/{doc.Id}")).StatusCode);

        await app.Factory.WithDbAsync(async db =>
        {
            Assert.False(await db.Set<KnowledgeDocument>().AnyAsync(d => d.Id == doc.Id));
            Assert.False(await db.Set<KnowledgeChunk>().AnyAsync(c => c.DocumentId == doc.Id));
        });
        Assert.DoesNotContain(app.Files.Files.Values, v => v.Contains("Parking permits"));
        Assert.Equal(HttpStatusCode.NotFound, (await admin.DeleteAsync($"/api/admin/knowledge/{doc.Id}")).StatusCode);
    }

    [Fact]
    public async Task SeededDemoDocument_IsReadyWithChunks_ButCannotBeReingested()
    {
        var (admin, _) = await app.SignInAsync(SisApiFactory.AdminId);
        var list = await (await admin.GetAsync("/api/admin/knowledge")).ReadAsync<List<KnowledgeDocumentDto>>();
        var seeded = list.Single(d => d.Title == "Academic policies (demo)");
        Assert.Equal(KnowledgeStatus.Ready, seeded.Status);
        Assert.True(seeded.Chunks > 0);
        Assert.False(seeded.CanReingest);

        var res = await admin.PostAsync($"/api/admin/knowledge/{seeded.Id}/reingest", null);
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Contains("file_missing", await res.Content.ReadAsStringAsync());
    }
}
