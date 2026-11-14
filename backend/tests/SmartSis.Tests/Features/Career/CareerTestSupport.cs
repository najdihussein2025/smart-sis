using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SmartSis.Api.Features.Career.Interfaces;
using SmartSis.Api.Features.Career.Models.Dtos;
using SmartSis.Tests.Infrastructure;

namespace SmartSis.Tests.Features.Career;

/// <summary>Shared helpers for the career API tests. Simulated data only (SR-23).</summary>
internal static class CareerTestSupport
{
    /// <summary>Loads the quiz and answers every question with the option at <paramref name="optionIndex"/>.</summary>
    public static async Task<(QuizDto Quiz, AnswerRequest[] Answers)> AnswerAllAsync(HttpClient student, int optionIndex = 0)
    {
        var quiz = await (await student.GetAsync("/api/career/quiz")).ReadAsync<QuizDto>();
        var answers = quiz.Questions.Select(q => new AnswerRequest(q.Id, q.Options[Math.Min(optionIndex, q.Options.Count - 1)].Id)).ToArray();
        return (quiz, answers);
    }

    public static async Task<HttpResponseMessage> SubmitAsync(HttpClient student, AnswerRequest[] answers) =>
        await student.PostAsJsonAsync("/api/career/assessments", new SubmitAssessmentRequest(answers));

    /// <summary>A new simulated student, signed in.</summary>
    public static async Task<HttpClient> NewStudentAsync(SisApiFactory factory, long universityId)
    {
        await factory.AddStudentAsync(universityId);
        return await factory.ClientForAsync(universityId);
    }

    /// <summary>Signs in on any app instance (e.g. one made with <see cref="WithAi"/>).</summary>
    public static async Task<HttpClient> SignInAsync(WebApplicationFactory<Program> app, long universityId)
    {
        var client = app.CreateClient();
        var res = await client.PostAsJsonAsync("/api/auth/login",
            new SmartSis.Api.Features.Auth.Models.Dtos.LoginRequest(universityId, SmartSis.Api.Shared.Data.Seed.DevSeeder.DevPassword));
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<SmartSis.Api.Features.Auth.Models.Dtos.LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", body!.AccessToken);
        return client;
    }

    /// <summary>The same app with the AI client replaced.</summary>
    public static WebApplicationFactory<Program> WithAi(SisApiFactory factory, ICareerAiClient ai) =>
        factory.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
        {
            s.RemoveAll<ICareerAiClient>();
            s.AddSingleton(ai);
        }));
}

/// <summary>Fake AI service: returns the given text (null = unavailable) and records what it was sent.</summary>
internal sealed class FakeCareerAi(string? reply) : ICareerAiClient
{
    public List<CareerExplainRequest> Requests { get; } = [];

    public Task<string?> ExplainAsync(CareerExplainRequest request, CancellationToken ct)
    {
        Requests.Add(request);
        return Task.FromResult(reply);
    }
}
