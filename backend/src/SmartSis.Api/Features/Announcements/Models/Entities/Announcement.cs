using SmartSis.Api.Features.Announcements.Enums;
using SmartSis.Api.Features.Lookups.Models.Entities;
using SmartSis.Api.Features.Sections.Models.Entities;
using SmartSis.Api.Features.Users.Models.Entities;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Announcements.Models.Entities;

public class Announcement : BaseEntity
{
    public int AuthorId { get; set; }
    public User Author { get; set; } = null!;
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public AnnouncementScope Scope { get; set; }

    /// <summary>Set only when Scope = Major.</summary>
    public int? MajorId { get; set; }
    public Major? Major { get; set; }

    /// <summary>Set only when Scope = Section.</summary>
    public int? SectionId { get; set; }
    public Section? Section { get; set; }

    public DateTime PublishAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
