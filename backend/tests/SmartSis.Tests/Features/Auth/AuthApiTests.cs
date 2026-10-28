using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Auth.Models.Dtos;
using SmartSis.Api.Features.Auth.Services;
using SmartSis.Api.Features.Notifications.Models.Entities;
using SmartSis.Api.Features.Users.Enums;
using SmartSis.Api.Features.Users.Models.Entities;
using SmartSis.Api.Shared.Auth;
using SmartSis.Api.Shared.Data.Seed;
using SmartSis.Tests.Infrastructure;

namespace SmartSis.Tests.Features.Auth;

public sealed class AuthApiTests(SisApiFactory factory) : IClassFixture<SisApiFactory>
{
    [Theory]
    [Trait("TC", "TC_01")]
    [InlineData(SisApiFactory.StudentId, "Student")]
    [InlineData(SisApiFactory.InstructorId, "Instructor")]
    [InlineData(SisApiFactory.AdminId, "Admin")]
    public async Task Login_ValidCredentials_ReturnsTokenAndRole(long universityId, string role)
    {
        var res = await factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest(universityId, DevSeeder.DevPassword));

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.False(string.IsNullOrWhiteSpace(body!.AccessToken));
        Assert.Equal(role, body.User.Role);
        Assert.Equal(universityId, body.User.UniversityId);
    }

    [Theory]
    [Trait("TC", "TC_02")]
    [InlineData(SisApiFactory.StudentId, "wrong-password")]
    [InlineData(999999, DevSeeder.DevPassword)]
    public async Task Login_BadCredentials_Returns401WithGenericMessage(long universityId, string password)
    {
        var res = await factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest(universityId, password));

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        Assert.Contains(AuthService.InvalidCredentials, await res.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Login_EmptyFields_Returns400()
    {
        var res = await factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest(0, ""));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Me_WithToken_ReturnsCurrentUser()
    {
        var client = await factory.ClientForAsync(SisApiFactory.StudentId);
        var me = await client.GetFromJsonAsync<AuthUserDto>("/api/auth/me");
        Assert.Equal(SisApiFactory.StudentId, me!.UniversityId);
    }

    [Fact]
    public async Task ForgotThenReset_ChangesPassword_AndLinkWorksOnce()
    {
        // Own user, so changing the password doesn't affect the other tests in this class.
        const long id = 4001;
        await factory.WithDbAsync(async db =>
        {
            db.Add(new User
            {
                UniversityId = id, Email = "reset.test@smartsis.local", FullName = "Reset Test",
                Role = UserRole.Student, PasswordHash = new PasswordHasher().Hash(DevSeeder.DevPassword),
            });
            await db.SaveChangesAsync();
        });
        var client = factory.CreateClient();

        var forgot = await client.PostAsJsonAsync("/api/auth/forgot-password", new ForgotPasswordRequest(id));
        Assert.Equal(HttpStatusCode.NoContent, forgot.StatusCode);

        string token = "";
        await factory.WithDbAsync(async db =>
        {
            var user = await db.Set<User>().SingleAsync(u => u.UniversityId == id);
            var mail = await db.Set<EmailOutboxMessage>().Where(m => m.ToEmail == user.Email).OrderByDescending(m => m.Id).FirstAsync();
            token = Regex.Match(mail.Body, "token=([A-Za-z0-9_-]+)").Groups[1].Value;
        });
        Assert.NotEmpty(token);

        var reset = await client.PostAsJsonAsync("/api/auth/reset-password", new ResetPasswordRequest(token, "NewPass123"));
        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);

        var again = await client.PostAsJsonAsync("/api/auth/reset-password", new ResetPasswordRequest(token, "Another123"));
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(id, "NewPass123"));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_UnknownId_StillReturns204()
    {
        var res = await factory.CreateClient().PostAsJsonAsync("/api/auth/forgot-password", new ForgotPasswordRequest(424242));
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
    }
}
