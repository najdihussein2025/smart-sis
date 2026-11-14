using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using SmartSis.Api.Features.Chat.Enums;
using SmartSis.Api.Features.Chat.Interfaces;
using SmartSis.Api.Shared.Exceptions;

namespace SmartSis.Api.Features.Chat.Services;

/// <summary>Settings for the Python AI service (section <c>AiService</c>).</summary>
public sealed class AiServiceOptions
{
    public const string Section = "AiService";

    public string BaseUrl { get; set; } = "http://localhost:8000";

    /// <summary>Shared secret sent as <c>X-Service-Key</c>. Must match <c>SERVICE_KEY</c> in the AI service.</summary>
    public string ServiceKey { get; set; } = "";

    /// <summary>The model may call several tools before it answers.</summary>
    public int TimeoutSeconds { get; set; } = 120;
}

/// <summary>
/// HTTP client for the Python AI service. Sends the shared service key, and for chat the student's own JWT as
/// <c>Authorization: Bearer</c>. Never logs the token or message text.
/// </summary>
public sealed class AiServiceClient(HttpClient http, IOptions<AiServiceOptions> options, ILogger<AiServiceClient> logger) : IAiServiceClient
{
    public const string Unavailable = "The assistant isn't available right now. Please try again in a few minutes.";
    private const string NotConfigured = "The assistant isn't set up yet. Please ask the administrator to configure it.";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public async Task<AiChatResult> ChatAsync(AiChatRequest request, string studentToken, CancellationToken ct)
    {
        var body = new
        {
            message = request.Message,
            history = request.History.Select(t => new { role = t.Role, content = t.Content }),
            mode = request.Mode == ChatMode.Tutor ? "tutor" : "assistant",
            course = request.Course is { } c ? new { code = c.Code, name = c.Name, description = c.Description } : null,
        };
        using var message = new HttpRequestMessage(HttpMethod.Post, "chat") { Content = JsonContent.Create(body, options: Json) };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", studentToken);
        var result = await SendAsync<ChatResponse>(message, "chat", ct);
        return new AiChatResult(result.Answer, result.ToolsUsed ?? []);
    }

    public async Task<int> IngestAsync(int documentId, string title, string content, CancellationToken ct)
    {
        var body = new { document_id = documentId, title, content };
        using var message = new HttpRequestMessage(HttpMethod.Post, "ingest") { Content = JsonContent.Create(body, options: Json) };
        var result = await SendAsync<IngestResponse>(message, "ingest", ct);
        return result.Chunks;
    }

    private async Task<T> SendAsync<T>(HttpRequestMessage message, string operation, CancellationToken ct)
    {
        message.Headers.Add("X-Service-Key", options.Value.ServiceKey);
        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(message, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            logger.LogWarning("AI service {Operation} could not be reached: {Error}", operation, ex.GetType().Name);
            throw new BusinessRuleException(Unavailable, "assistant_unavailable");
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("AI service {Operation} answered {Status}", operation, (int)response.StatusCode);
                throw new BusinessRuleException(
                    response.StatusCode == HttpStatusCode.ServiceUnavailable ? NotConfigured : Unavailable, "assistant_unavailable");
            }
            var result = await response.Content.ReadFromJsonAsync<T>(Json, ct);
            return result ?? throw new BusinessRuleException(Unavailable, "assistant_unavailable");
        }
    }

    private sealed record ChatResponse(string Answer, [property: JsonPropertyName("tools_used")] List<string>? ToolsUsed);

    private sealed record IngestResponse([property: JsonPropertyName("document_id")] int DocumentId, int Chunks);
}
