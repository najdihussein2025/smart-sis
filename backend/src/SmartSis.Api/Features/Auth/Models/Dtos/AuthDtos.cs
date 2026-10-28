namespace SmartSis.Api.Features.Auth.Models.Dtos;

public sealed record LoginRequest(long UniversityId, string Password);

public sealed record LoginResponse(string AccessToken, DateTime ExpiresAt, AuthUserDto User);

public sealed record AuthUserDto(int Id, long UniversityId, string FullName, string Role, string? PhotoUrl);

public sealed record ForgotPasswordRequest(long UniversityId);

public sealed record ResetPasswordRequest(string Token, string NewPassword);
