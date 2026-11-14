using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Chat.Enums;
using SmartSis.Api.Features.Chat.Models.Dtos;
using SmartSis.Api.Features.Chat.Models.Entities;
using SmartSis.Api.Features.Terms.Models.Dtos;
using SmartSis.Tests.Infrastructure;

namespace SmartSis.Tests.Features.Chat;

/// <summary>
/// T62 / T64 / T71: the chat API saves conversations per student, forwards only the caller's own JWT to the AI service,
/// and never lets one student reach another student's conversation. Simulated data only (SR-23).
/// </summary>
public sealed class ChatApiTests(ChatTestApp app) : IClassFixture<ChatTestApp>
{
    private const long StudentA = SisApiFactory.StudentId; // 3001: enrolled in CSC 210 A and MAT 201 A
    private const long StudentB = 3002;

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, string message, int? conversationId = null, ChatMode? mode = null, int? courseId = null) =>
        client.PostAsJsonAsync("/api/chat/messages", new SendChatMessageRequest(message, conversationId, mode, courseId), TestData.Json);

    private async Task<int> CourseIdAsync(string code)
    {
        var id = 0;
        await app.Factory.WithDbAsync(async db => id = await db.Set<SmartSis.Api.Features.Courses.Models.Entities.Course>()
            .Where(c => c.Code == code).Select(c => c.Id).SingleAsync());
        return id;
    }

    [Fact]
    public async Task Send_NewConversation_SavesBothMessages_AndForwardsCallersOwnToken()
    {
        app.Ai.Reset();
        var (client, token) = await app.SignInAsync(StudentA);

        var res = await SendAsync(client, "  When is my next class?  ");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var reply = await res.ReadAsync<ChatReplyDto>();
        Assert.Equal("When is my next class?", reply.UserMessage.Content);
        Assert.Equal("Answer to: When is my next class?", reply.AssistantMessage.Content);
        Assert.Equal(ChatRole.Assistant, reply.AssistantMessage.Role);
        Assert.Equal("When is my next class?", reply.Conversation.Title);
        Assert.Equal(ChatMode.Assistant, reply.Conversation.Mode);

        var call = Assert.Single(app.Ai.Calls);
        Assert.Equal(token, call.Token);
        Assert.Empty(call.Request.History);
        Assert.Null(call.Request.Course);

        var saved = await (await client.GetAsync($"/api/chat/conversations/{reply.Conversation.Id}")).ReadAsync<ConversationDto>();
        Assert.Equal([ChatRole.User, ChatRole.Assistant], saved.Messages.Select(m => m.Role));
    }

    [Fact]
    public async Task Send_ExistingConversation_ForwardsHistoryInOrder()
    {
        app.Ai.Reset();
        var (client, _) = await app.SignInAsync(StudentA);
        var first = await (await SendAsync(client, "First question")).ReadAsync<ChatReplyDto>();

        await SendAsync(client, "Second question", first.Conversation.Id);

        var second = app.Ai.Calls.Last();
        Assert.Equal("Second question", second.Request.Message);
        Assert.Equal(["user", "assistant"], second.Request.History.Select(h => h.Role));
        Assert.Equal(["First question", "Answer to: First question"], second.Request.History.Select(h => h.Content));
    }

    [Fact]
    public async Task OtherStudent_CannotReadRenameDeleteOrContinue_Returns404_AndAiIsNotCalled()
    {
        app.Ai.Reset();
        var (a, _) = await app.SignInAsync(StudentA);
        var (b, _) = await app.SignInAsync(StudentB);
        var mine = await (await SendAsync(a, "My private question")).ReadAsync<ChatReplyDto>();
        var id = mine.Conversation.Id;
        app.Ai.Calls.Clear();

        Assert.Equal(HttpStatusCode.NotFound, (await b.GetAsync($"/api/chat/conversations/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.PutAsJsonAsync($"/api/chat/conversations/{id}", new RenameConversationRequest("Hijacked"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.DeleteAsync($"/api/chat/conversations/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(b, "Tell me what you told A", id)).StatusCode);
        Assert.Empty(app.Ai.Calls);

        var listB = await (await b.GetAsync("/api/chat/conversations")).ReadAsync<List<ConversationSummaryDto>>();
        Assert.DoesNotContain(listB, c => c.Id == id);
        var stillA = await (await a.GetAsync($"/api/chat/conversations/{id}")).ReadAsync<ConversationDto>();
        Assert.Equal("My private question", stillA.Conversation.Title);
    }

    [Fact]
    public async Task EachStudentsMessages_GoToTheAiWithTheirOwnToken()
    {
        app.Ai.Reset();
        var (a, tokenA) = await app.SignInAsync(StudentA);
        var (b, tokenB) = await app.SignInAsync(StudentB);
        Assert.NotEqual(tokenA, tokenB);

        await SendAsync(a, "What are my grades? (A)");
        await SendAsync(b, "What are my grades? (B)");

        var calls = app.Ai.Calls.ToList();
        Assert.Equal(tokenA, calls.Single(c => c.Request.Message.EndsWith("(A)")).Token);
        Assert.Equal(tokenB, calls.Single(c => c.Request.Message.EndsWith("(B)")).Token);
    }

    [Fact]
    public async Task Rename_ThenDelete_SoftDeletes_AndHidesConversation()
    {
        app.Ai.Reset();
        var (client, _) = await app.SignInAsync(StudentA);
        var reply = await (await SendAsync(client, "Something to delete")).ReadAsync<ChatReplyDto>();
        var id = reply.Conversation.Id;

        var renamed = await (await client.PutAsJsonAsync($"/api/chat/conversations/{id}", new RenameConversationRequest("  Exam prep  "))).ReadAsync<ConversationSummaryDto>();
        Assert.Equal("Exam prep", renamed.Title);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/chat/conversations/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/chat/conversations/{id}")).StatusCode);
        var list = await (await client.GetAsync("/api/chat/conversations")).ReadAsync<List<ConversationSummaryDto>>();
        Assert.DoesNotContain(list, c => c.Id == id);

        await app.Factory.WithDbAsync(async db =>
        {
            var row = await db.Set<ChatConversation>().IgnoreQueryFilters().SingleAsync(c => c.Id == id);
            Assert.NotNull(row.DeletedAt); // D1: kept, marked deleted
        });
    }

    [Fact]
    public async Task Rename_EmptyTitle_Returns400()
    {
        var (client, _) = await app.SignInAsync(StudentA);
        var reply = await (await SendAsync(client, "Title test")).ReadAsync<ChatReplyDto>();
        var res = await client.PutAsJsonAsync($"/api/chat/conversations/{reply.Conversation.Id}", new RenameConversationRequest(" "));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Send_EmptyMessage_Returns400()
    {
        var (client, _) = await app.SignInAsync(StudentA);
        Assert.Equal(HttpStatusCode.BadRequest, (await SendAsync(client, "   ")).StatusCode);
    }

    [Fact]
    public async Task Send_AiUnavailable_Returns409_AndSavesNothing()
    {
        app.Ai.Reset();
        var (client, _) = await app.SignInAsync(StudentB);
        var before = (await (await client.GetAsync("/api/chat/conversations")).ReadAsync<List<ConversationSummaryDto>>()).Count;
        app.Ai.Down = true;

        var res = await SendAsync(client, "Is anyone there?");

        app.Ai.Down = false;
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Contains("assistant_unavailable", await res.Content.ReadAsStringAsync());
        var after = await (await client.GetAsync("/api/chat/conversations")).ReadAsync<List<ConversationSummaryDto>>();
        Assert.Equal(before, after.Count);
    }

    [Fact]
    [Trait("Task", "T71")]
    public async Task Tutor_EnrolledCourse_RemembersModeAndCourse_AndSendsCourseToAi()
    {
        app.Ai.Reset();
        var (client, _) = await app.SignInAsync(StudentA);
        var csc210 = await CourseIdAsync("CSC 210");

        var reply = await (await SendAsync(client, "Explain linked lists", mode: ChatMode.Tutor, courseId: csc210)).ReadAsync<ChatReplyDto>();

        Assert.Equal(ChatMode.Tutor, reply.Conversation.Mode);
        Assert.Equal(csc210, reply.Conversation.CourseId);
        Assert.Equal("CSC 210", reply.Conversation.CourseCode);
        Assert.Equal("CSC 210: Explain linked lists", reply.Conversation.Title);
        var first = app.Ai.Calls.Last();
        Assert.Equal(ChatMode.Tutor, first.Request.Mode);
        Assert.Equal("CSC 210", first.Request.Course?.Code);

        // A follow-up keeps tutor mode and course; the hidden context message is never sent as history or shown.
        await SendAsync(client, "And doubly linked?", reply.Conversation.Id);
        var follow = app.Ai.Calls.Last();
        Assert.Equal(ChatMode.Tutor, follow.Request.Mode);
        Assert.Equal("CSC 210", follow.Request.Course?.Code);
        Assert.Equal(["user", "assistant"], follow.Request.History.Select(h => h.Role));

        var listed = (await (await client.GetAsync("/api/chat/conversations")).ReadAsync<List<ConversationSummaryDto>>())
            .Single(c => c.Id == reply.Conversation.Id);
        Assert.Equal(ChatMode.Tutor, listed.Mode);
        Assert.Equal("CSC 210", listed.CourseCode);
        var full = await (await client.GetAsync($"/api/chat/conversations/{reply.Conversation.Id}")).ReadAsync<ConversationDto>();
        Assert.DoesNotContain(full.Messages, m => m.Role == ChatRole.Tool);
    }

    [Fact]
    [Trait("Task", "T71")]
    public async Task Tutor_CourseNotEnrolled_Returns409()
    {
        app.Ai.Reset();
        var (client, _) = await app.SignInAsync(StudentA);
        var csc101 = await CourseIdAsync("CSC 101"); // completed last term, not enrolled now

        var res = await SendAsync(client, "Explain loops", mode: ChatMode.Tutor, courseId: csc101);

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Contains("not_enrolled", await res.Content.ReadAsStringAsync());
        Assert.Empty(app.Ai.Calls);
    }

    [Fact]
    [Trait("Task", "T71")]
    public async Task Tutor_WithoutCourse_Returns400()
    {
        var (client, _) = await app.SignInAsync(StudentA);
        Assert.Equal(HttpStatusCode.BadRequest, (await SendAsync(client, "Explain recursion", mode: ChatMode.Tutor)).StatusCode);
    }

    [Fact]
    public async Task CurrentTerm_AnySignedInRole_Returns200()
    {
        foreach (var id in new[] { StudentA, SisApiFactory.InstructorId, SisApiFactory.AdminId })
        {
            var (client, _) = await app.SignInAsync(id);
            var res = await client.GetAsync("/api/terms/current");
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            var term = await res.ReadAsync<TermDto>();
            Assert.True(term.IsCurrent);
        }
    }
}
