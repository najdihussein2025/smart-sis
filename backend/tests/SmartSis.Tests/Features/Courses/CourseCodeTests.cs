using SmartSis.Api.Features.Courses.Rules;

namespace SmartSis.Tests.Features.Courses;

[Trait("SR", "SR-21")]
public sealed class CourseCodeTests
{
    [Theory]
    [InlineData("csc101", "CSC 101")]
    [InlineData("CSC 101", "CSC 101")]
    [InlineData("  mat 201  ", "MAT 201")]
    [InlineData("ab123", "AB 123")]
    [InlineData("PHYSX110l", "PHYSX 110L")]
    public void Normalize_Valid_UpperCaseWithOneSpace(string input, string expected)
    {
        Assert.True(CourseCode.IsValid(input));
        Assert.Equal(expected, CourseCode.Normalize(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("C 101")]       // 1 letter
    [InlineData("ABCDEF 101")]  // 6 letters
    [InlineData("CSC 10")]      // 2 digits
    [InlineData("CSC 1011")]    // 4 digits
    [InlineData("CSC  101")]    // two spaces
    [InlineData("CSC-101")]
    [InlineData("CSC 101AB")]
    public void IsValid_Invalid_ReturnsFalse(string? input) => Assert.False(CourseCode.IsValid(input));

    [Fact]
    public void Normalize_Invalid_Throws() => Assert.Throws<ArgumentException>(() => CourseCode.Normalize("nope"));
}
