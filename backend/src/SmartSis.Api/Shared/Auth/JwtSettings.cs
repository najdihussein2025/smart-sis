namespace SmartSis.Api.Shared.Auth;

public sealed class JwtSettings
{
    public const string Section = "Jwt";

    public string Issuer { get; set; } = "SmartSis";
    public string Audience { get; set; } = "SmartSis.Web";
    public string Key { get; set; } = "";
    public int ExpiresMinutes { get; set; } = 480;
}
