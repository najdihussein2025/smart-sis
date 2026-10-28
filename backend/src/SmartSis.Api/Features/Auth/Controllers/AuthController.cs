using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSis.Api.Features.Auth.Interfaces;
using SmartSis.Api.Features.Auth.Models.Dtos;

namespace SmartSis.Api.Features.Auth.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService auth) : ControllerBase
{
    /// <summary>TC_01–TC_06: login with University ID + password.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public Task<LoginResponse> Login(LoginRequest request, CancellationToken ct) => auth.LoginAsync(request, ct);

    [HttpGet("me")]
    public Task<AuthUserDto> Me(CancellationToken ct) => auth.GetCurrentAsync(ct);

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken ct)
    {
        await auth.RequestPasswordResetAsync(request, ct);
        return NoContent();
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken ct)
    {
        await auth.ResetPasswordAsync(request, ct);
        return NoContent();
    }
}
