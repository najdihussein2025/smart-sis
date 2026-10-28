namespace SmartSis.Api.Shared.Auth;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

/// <summary>BCrypt, work factor 11.</summary>
public sealed class PasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 11;

    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string hash)
    {
        try { return BCrypt.Net.BCrypt.Verify(password, hash); }
        catch (Exception ex) when (ex is BCrypt.Net.SaltParseException or ArgumentException) { return false; }
    }
}
