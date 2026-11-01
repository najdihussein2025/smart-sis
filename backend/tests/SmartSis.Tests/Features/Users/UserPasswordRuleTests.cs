using Microsoft.Extensions.Time.Testing;
using SmartSis.Api.Features.Auth.Models.Dtos;
using SmartSis.Api.Features.Auth.Validators;
using SmartSis.Api.Features.Users.Enums;
using SmartSis.Api.Features.Users.Models.Dtos;
using SmartSis.Api.Features.Users.Validators;

namespace SmartSis.Tests.Features.Users;

/// <summary>
/// The initial-password rule is a copy of Auth's reset-password rule (Auth can't be edited from Users).
/// This keeps the two in sync: if one changes, this test fails.
/// </summary>
[Trait("SR", "SR-20")]
public sealed class UserPasswordRuleTests
{
    private static readonly FakeTimeProvider Clock = new(new DateTimeOffset(2026, 11, 1, 12, 0, 0, TimeSpan.Zero));

    [Theory]
    [InlineData("Initial123")]
    [InlineData("a1234567")]
    [InlineData("abcdefg1")]
    [InlineData("Ab1")]
    [InlineData("abcdefghij")]
    [InlineData("1234567890")]
    [InlineData("")]
    [InlineData("        ")]
    [InlineData("pass word 1")]
    public void InitialPassword_SameRuleAsResetPassword(string password)
    {
        var reset = new ResetPasswordRequestValidator().Validate(new ResetPasswordRequest("token", password));
        var create = new CreateUserRequestValidator(Clock).Validate(new CreateUserRequest(
            5001, "x@test.smartsis.local", "Simulated", null, null, UserRole.Instructor, password, null, new InstructorInfoRequest(1)));

        var resetOk = reset.Errors.All(e => e.PropertyName != nameof(ResetPasswordRequest.NewPassword));
        var createOk = create.Errors.All(e => e.PropertyName != nameof(CreateUserRequest.InitialPassword));
        Assert.Equal(resetOk, createOk);
    }

    [Fact]
    public void DateOfBirth_UsesInjectedClock()
    {
        var today = DateOnly.FromDateTime(Clock.GetUtcNow().UtcDateTime);
        CreateUserRequest With(DateOnly dob) => new(5001, "x@test.smartsis.local", "Simulated", null, dob,
            UserRole.Instructor, "Initial123", null, new InstructorInfoRequest(1));

        var v = new CreateUserRequestValidator(Clock);
        Assert.True(v.Validate(With(today.AddDays(-1))).IsValid);
        Assert.False(v.Validate(With(today)).IsValid);
    }
}
