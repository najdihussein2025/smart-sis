using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SmartSis.Api.Features.Career.Clients;
using SmartSis.Api.Features.Career.Interfaces;

namespace SmartSis.Tests.Features.Career;

/// <summary>PB-17 / T66: the .NET side of POST /career/explain never throws; any failure means "use the fallback".</summary>
public sealed class CareerAiClientTests
{
    private static readonly CareerExplainRequest Request = new(
        [new CareerExplainCareer("Data Scientist", 80, 75, 92, ["Analytical thinking"])],
        [new CareerExplainTrait("Analytical thinking", 75)],
        ["CSC 101 Introduction to Programming"],
        UsesGrades: true);

    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<(HttpRequestMessage Request, string Body)> Calls { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls.Add((request, request.Content is null ? "" : await request.Content.ReadAsStringAsync(ct)));
            return respond(request);
        }
    }

    private static (CareerAiClient Client, Handler Handler) Create(Func<HttpRequestMessage, HttpResponseMessage> respond, string key = "secret")
    {
        var handler = new Handler(respond);
        var options = Options.Create(new CareerAiOptions { BaseUrl = "http://ai.test:8000", ServiceKey = key });
        return (new CareerAiClient(new HttpClient(handler), options, NullLogger<CareerAiClient>.Instance), handler);
    }

    private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    [Fact]
    public async Task Explain_LlmAnswer_ReturnsText_AndSendsServiceKey()
    {
        var (client, handler) = Create(_ => Json("""{"explanation":"You love data.","source":"llm"}"""));

        Assert.Equal("You love data.", await client.ExplainAsync(Request, CancellationToken.None));

        var (req, body) = Assert.Single(handler.Calls);
        Assert.Equal("http://ai.test:8000/career/explain", req.RequestUri!.ToString());
        Assert.Equal("secret", req.Headers.GetValues("X-Service-Key").Single());
        Assert.Contains("\"matchPct\":80", body);
    }

    [Theory]
    [InlineData("""{"explanation":"Built in.","source":"fallback"}""", HttpStatusCode.OK)]
    [InlineData("""{"explanation":"","source":"llm"}""", HttpStatusCode.OK)]
    [InlineData("""{"detail":"Invalid service key."}""", HttpStatusCode.Unauthorized)]
    [InlineData("not json", HttpStatusCode.OK)]
    public async Task Explain_NotAnLlmAnswer_ReturnsNull(string json, HttpStatusCode status)
    {
        var (client, _) = Create(_ => Json(json, status));
        Assert.Null(await client.ExplainAsync(Request, CancellationToken.None));
    }

    [Fact]
    public async Task Explain_ServiceDown_ReturnsNull()
    {
        var (client, _) = Create(_ => throw new HttpRequestException("Connection refused"));
        Assert.Null(await client.ExplainAsync(Request, CancellationToken.None));
    }

    [Fact]
    public async Task Explain_NoServiceKey_DoesNotCall()
    {
        var (client, handler) = Create(_ => Json("{}"), key: "");
        Assert.Null(await client.ExplainAsync(Request, CancellationToken.None));
        Assert.Empty(handler.Calls);
    }
}
