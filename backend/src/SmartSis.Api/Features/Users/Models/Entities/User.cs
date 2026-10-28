using SmartSis.Api.Features.Users.Enums;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Users.Models.Entities;

public class User : BaseEntity, ISoftDeletable
{
    /// <summary>Numeric university ID used to log in. Unique.</summary>
    public long UniversityId { get; set; }
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public UserRole Role { get; set; }
    public string FullName { get; set; } = "";
    public string? Phone { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? PhotoUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? DeletedAt { get; set; }

    public Student? Student { get; set; }
    public Instructor? Instructor { get; set; }

    public bool CanSignIn => IsActive && DeletedAt is null;

    public void MarkDeleted(DateTime utcNow)
    {
        IsActive = false;
        DeletedAt = utcNow;
    }
}
