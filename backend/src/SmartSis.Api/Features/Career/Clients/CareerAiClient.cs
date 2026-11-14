using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using SmartSis.Api.Features.Career.Interfaces;

namespace SmartSis.Api.Features.Career.Clients;

/// <summary>The <c>AiService</c> settings the career module uses.</summary>
public sealed class CareerAiOptions
{
    public const string Section = "AiService";

    /// <summary>Base URL of the Python AI service, e.g. http://localhost:8000.</summary>
    public string BaseUrl { get; set; } = "";

    /// <summary>Shared secret sent as <c>X-Service-Key</c>. Empty = the AI service is not used.</summary>
    public string ServiceKey { get; set; } = "";
}

/// <summary>
/// Calls <c>POST /career/explain</c> on the AI service. Any failure (not configured, unreachable, timeout, non-2xx,
/// LLM unavailable on the Python side) returns null, and the caller uses the built-in explanation.
/// </summary>
public sealed class CareerAiClient(HttpClient http, IOptions<CareerAiOptions> options, ILogger<CareerAiClient> logger) : ICareerAiClient
{
    /// <summary>The Python service reports "llm" when the model wrote the text.</summary>
    private const string LlmSource = "llm";

    public async Task<string?> ExplainAsync(CareerExplainRequest request, CancellationToken ct)
    {
        var o = options.Value;
        if (string.IsNullOrWhiteSpace(o.BaseUrl) || string.IsNullOrWhiteSpace(o.ServiceKey)) return null;

        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(o.BaseUrl.TrimEnd('/') + "/"), "career/explain"))
            {
                Content = JsonContent.Create(request),
            };
            message.Headers.Add("X-Service-Key", o.ServiceKey);

            using var response = await http.SendAsync(message, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("AI service /career/explain returned {Status}; using the built-in explanation.", (int)response.StatusCode);
                return null;
            }

            var body = await response.Content.ReadFromJsonAsync<ExplainResponse>(ct);
            return body is { Source: LlmSource } && !string.IsNullOrWhiteSpace(body.Explanation) ? body.Explanation : null;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException or NotSupportedException)
        {
            if (ct.IsCancellationRequested) throw;
            logger.LogWarning("AI service /career/explain failed ({Error}); using the built-in explanation.", ex.GetType().Name);
            return null;
        }
    }

    private sealed record ExplainResponse(string? Explanation, string? Source);
}
