using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SmartSis.Api.Features.Chat.Enums;
using SmartSis.Api.Features.Chat.Interfaces;
using SmartSis.Api.Features.Chat.Models;
using SmartSis.Api.Features.Chat.Services;
using SmartSis.Api.Shared.Exceptions;

namespace SmartSis.Tests.Features.Chat;

/// <summary>T64: what exactly goes over the wire to the Python service, and how failures reach the student.</summary>
public sealed class AiServiceClientTests
{
    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Request = request;
            RequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            return new HttpResponseMessage(status) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };
        }
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            throw new HttpRequestException("connection refused");
    }

    private static AiServiceClient Client(HttpMessageHandler handler) => new(
        new HttpClient(handler) { BaseAddress = new Uri("http://ai.test/") },
        Options.Create(new AiServiceOptions { ServiceKey = "svc-key" }),
        NullLogger<AiServiceClient>.Instance);

    [Fact]
    public async Task Chat_SendsServiceKey_StudentToken_AndSnakeCaseBody()
    {
        var handler = new StubHandler(HttpStatusCode.OK, """{"answer":"Hi","tools_used":["get_my_grades"]}""");
        var course = new TutorCourse(5, "CSC 210", "Data Structures", "Lists and trees");

        var result = await Client(handler).ChatAsync(
            new AiChatRequest("Explain trees", [new AiTurn("user", "q"), new AiTurn("assistant", "a")], ChatMode.Tutor, course),
            "student-jwt", CancellationToken.None);

        Assert.Equal("Hi", result.Answer);
        Assert.Equal(["get_my_grades"], result.ToolsUsed);
        var req = handler.Request!;
        Assert.Equal("http://ai.test/chat", req.RequestUri!.ToString());
        Assert.Equal("Bearer", req.Headers.Authorization!.Scheme);
        Assert.Equal("student-jwt", req.Headers.Authorization.Parameter);
        Assert.Equal("svc-key", req.Headers.GetValues("X-Service-Key").Single());

        using var json = JsonDocument.Parse(handler.RequestBody!);
        var root = json.RootElement;
        Assert.Equal("Explain trees", root.GetProperty("message").GetString());
        Assert.Equal("tutor", root.GetProperty("mode").GetString());
        Assert.Equal("CSC 210", root.GetProperty("course").GetProperty("code").GetString());
        Assert.Equal(2, root.GetProperty("history").GetArrayLength());
        Assert.False(root.TryGetProperty("token", out _)); // the token travels only in the header
    }

    [Fact]
    public async Task Ingest_SendsDocumentInline_WithoutAnyStudentToken()
    {
        var handler = new StubHandler(HttpStatusCode.OK, """{"document_id":7,"chunks":3}""");
        var chunks = await Client(handler).IngestAsync(7, "FAQ", "Some text", CancellationToken.None);

        Assert.Equal(3, chunks);
        Assert.Null(handler.Request!.Headers.Authorization);
        using var json = JsonDocument.Parse(handler.RequestBody!);
        Assert.Equal(7, json.RootElement.GetProperty("document_id").GetInt32());
        Assert.Equal("Some text", json.RootElement.GetProperty("content").GetString());
    }

    [Fact]
    public async Task Chat_ServiceNotConfigured503_ThrowsFriendlyMessage()
    {
        var handler = new StubHandler(HttpStatusCode.ServiceUnavailable, """{"detail":"LLM_API_KEY is not set"}""");
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            Client(handler).ChatAsync(new AiChatRequest("hi", [], ChatMode.Assistant, null), "t", CancellationToken.None));
        Assert.Equal("assistant_unavailable", ex.Code);
        Assert.Contains("isn't set up", ex.Message);
    }

    [Fact]
    public async Task Chat_ServiceDown_ThrowsFriendlyMessage()
    {
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            Client(new ThrowingHandler()).ChatAsync(new AiChatRequest("hi", [], ChatMode.Assistant, null), "t", CancellationToken.None));
        Assert.Equal("assistant_unavailable", ex.Code);
        Assert.Equal(AiServiceClient.Unavailable, ex.Message);
    }

    [Fact]
    public void ConversationContext_RoundTrips_AndFallsBackToAssistant()
    {
        var ctx = new ConversationContext(ChatMode.Tutor, 5, "CSC 210", "Data Structures");
        Assert.Equal(ctx, ConversationContext.Parse(ctx.Serialize()));
        Assert.Equal(ConversationContext.Assistant, ConversationContext.Parse(null));
        Assert.Equal(ConversationContext.Assistant, ConversationContext.Parse("not json"));
        Assert.Equal(ConversationContext.Assistant, ConversationContext.Parse("""{"kind":"something-else"}"""));
    }

    [Fact]
    public void MakeTitle_ShortensLongMessages_AndPrefixesTutorCourse()
    {
        var longText = new string('a', 100);
        var title = ChatService.MakeTitle(longText, ConversationContext.Assistant);
        Assert.Equal(60, title.Length);
        Assert.EndsWith("…", title);
        Assert.Equal("What is   GPA?".Replace("   ", " "), ChatService.MakeTitle("What is \n  GPA?", ConversationContext.Assistant));
        Assert.Equal("CSC 210: Trees", ChatService.MakeTitle("Trees", new ConversationContext(ChatMode.Tutor, 5, "CSC 210", "DS")));
    }
}
