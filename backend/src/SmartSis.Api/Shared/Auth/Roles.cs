namespace SmartSis.Api.Shared.Auth;

/// <summary>Role names as they appear in the JWT "role" claim and in Users.Role.</summary>
public static class Roles
{
    public const string Student = "Student";
    public const string Instructor = "Instructor";
    public const string Admin = "Admin";
}

/// <summary>Authorization policy names. Use on controllers: [Authorize(Policy = Policies.Student)].</summary>
public static class Policies
{
    public const string Student = "StudentOnly";
    public const string Instructor = "InstructorOnly";
    public const string Admin = "AdminOnly";
    public const string Staff = "InstructorOrAdmin";
}

/// <summary>Claim types we put in our tokens (short JWT names, inbound mapping is off).</summary>
public static class SisClaims
{
    public const string UserId = "sub";
    public const string UniversityId = "uid";
    public const string Name = "name";
    public const string Role = "role";
}
