using SmartSis.Api.Features.Sections.Models.Dtos;

namespace SmartSis.Api.Features.Cart.Models.Dtos;

/// <summary>The signed-in student's cart for the current term.</summary>
/// <param name="TermId">Current term id, or null when no term is current.</param>
/// <param name="TermName">Current term name.</param>
/// <param name="Window">"Closed", "Enrollment" or "AddDrop".</param>
/// <param name="Items">Sections in the cart, oldest first.</param>
/// <param name="CartCredits">Credits of the sections in the cart.</param>
/// <param name="EnrolledCredits">Credits the student is already enrolled in this term.</param>
/// <param name="MaxCredits">The student's credit cap per term (rule E2).</param>
public sealed record CartDto(
    int? TermId, string? TermName, string Window, IReadOnlyList<CartItemDto> Items, int CartCredits, int EnrolledCredits, int MaxCredits);

/// <summary>A section in the cart.</summary>
/// <param name="SectionId">Section id.</param>
/// <param name="CourseId">Course id.</param>
/// <param name="CourseCode">Course code.</param>
/// <param name="CourseName">Course name.</param>
/// <param name="Credits">Course credits.</param>
/// <param name="SectionCode">Section code.</param>
/// <param name="InstructorName">Instructor, or null.</param>
/// <param name="Capacity">Maximum seats.</param>
/// <param name="SeatsLeft">Free seats right now.</param>
/// <param name="Meetings">Weekly meetings.</param>
/// <param name="AddedAt">When it was added (UTC).</param>
public sealed record CartItemDto(
    int SectionId,
    int CourseId,
    string CourseCode,
    string CourseName,
    int Credits,
    string SectionCode,
    string? InstructorName,
    int Capacity,
    int SeatsLeft,
    IReadOnlyList<SectionMeetingDto> Meetings,
    DateTime AddedAt);

/// <summary>Add a section to the cart.</summary>
/// <param name="SectionId">Section of the current term.</param>
public sealed record AddToCartRequest(int SectionId);
