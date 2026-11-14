using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SmartSis.Api.Features.Auth.Models.Dtos;
using SmartSis.Api.Features.Chat.Interfaces;
using SmartSis.Api.Shared.Data.Seed;
using SmartSis.Api.Shared.Exceptions;
using SmartSis.Tests.Infrastructure;

namespace SmartSis.Tests.Features.Chat;

/// <summary>
/// The real API (EF InMemory, seeded) with the Python AI service and the file store replaced by fakes, so tests can see
/// exactly which token and history the API forwards.
/// </summary>
public sealed class ChatTestApp : IDisposable
{
    public SisApiFactory Factory { get; } = new();
    public FakeAiService Ai { get; } = new();
    public FakeFileStore Files { get; } = new();
    public WebApplicationFactory<Program> App { get; }

    public ChatTestApp()
    {
        App = Factory.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
        {
            s.RemoveAll<IAiServiceClient>();
            s.AddSingleton<IAiServiceClient>(Ai);
            s.RemoveAll<IKnowledgeFileStore>();
            s.AddSingleton<IKnowledgeFileStore>(Files);
        }));
    }

    /// <summary>A signed-in client and the raw JWT it sends.</summary>
    public async Task<(HttpClient Client, string Token)> SignInAsync(long universityId)
    {
        var client = App.CreateClient();
        var res = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(universityId, DevSeeder.DevPassword));
        res.EnsureSuccessStatusCode();
        var token = (await res.Content.ReadFromJsonAsync<LoginResponse>())!.AccessToken;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (client, token);
    }

    public void Dispose()
    {
        App.Dispose();
        Factory.Dispose();
    }
}

public sealed record AiCall(AiChatRequest Request, string Token);

public sealed class FakeAiService : IAiServiceClient
{
    public ConcurrentQueue<AiCall> Calls { get; } = new();
    public ConcurrentQueue<(int DocumentId, string Title, string Content)> Ingests { get; } = new();
    public bool Down { get; set; }

    public Task<AiChatResult> ChatAsync(AiChatRequest request, string studentToken, CancellationToken ct)
    {
        if (Down) throw new BusinessRuleException("The assistant isn't available right now. Please try again in a few minutes.", "assistant_unavailable");
        Calls.Enqueue(new AiCall(request, studentToken));
        return Task.FromResult(new AiChatResult($"Answer to: {request.Message}", ["get_my_schedule"]));
    }

    public Task<int> IngestAsync(int documentId, string title, string content, CancellationToken ct)
    {
        if (Down) throw new BusinessRuleException("The assistant isn't available right now. Please try again in a few minutes.", "assistant_unavailable");
        Ingests.Enqueue((documentId, title, content));
        return Task.FromResult(1);
    }

    public void Reset()
    {
        Down = false;
        Calls.Clear();
        Ingests.Clear();
    }
}

public sealed class FakeFileStore : IKnowledgeFileStore
{
    public ConcurrentDictionary<string, string> Files { get; } = new();

    public Task<string> SaveAsync(string content, string extension, CancellationToken ct)
    {
        var key = $"{Guid.NewGuid():N}{extension}";
        Files[key] = content;
        return Task.FromResult(key);
    }

    public Task<string?> ReadAsync(string key, CancellationToken ct) => Task.FromResult(Files.TryGetValue(key, out var c) ? c : null);

    public void Delete(string key) => Files.TryRemove(key, out _);
}
