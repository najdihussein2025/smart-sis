using SmartSis.Api.Features.Courses.Rules;

namespace SmartSis.Tests.Features.Courses;

/// <summary>Edges are (course, prerequisite): (A, B) = A requires B.</summary>
[Trait("SR", "SR-21")]
public sealed class PrerequisiteCycleCheckTests
{
    private const int A = 1, B = 2, C = 3, D = 4;

    [Fact]
    public void FindCycle_NoEdges_ReturnsNull() => Assert.Null(PrerequisiteCycleCheck.FindCycle([]));

    [Fact]
    public void FindCycle_SimpleChain_ReturnsNull() =>
        Assert.Null(PrerequisiteCycleCheck.FindCycle([(C, B), (B, A)]));

    [Fact]
    public void FindCycle_SelfLoop_ReturnsPath() =>
        Assert.Equal([A, A], PrerequisiteCycleCheck.FindCycle([(A, A)]));

    [Fact]
    public void FindCycle_DirectPair_ReturnsPath() =>
        Assert.Equal([A, B, A], PrerequisiteCycleCheck.FindCycle([(A, B), (B, A)]));

    [Fact]
    public void FindCycle_Indirect_ReturnsPath() =>
        Assert.Equal([A, B, C, A], PrerequisiteCycleCheck.FindCycle([(A, B), (B, C), (C, A)]));

    [Fact]
    public void FindCycle_Diamond_ReturnsNull() =>
        Assert.Null(PrerequisiteCycleCheck.FindCycle([(A, B), (A, C), (B, D), (C, D)]));

    [Fact]
    public void FindCycle_CycleAwayFromFirstNode_IsFound() =>
        Assert.Equal([B, C, B], PrerequisiteCycleCheck.FindCycle([(A, B), (B, C), (C, B)]));

    [Fact]
    public void FindCycleThrough_NoEdges_ReturnsNull() => Assert.Null(PrerequisiteCycleCheck.FindCycleThrough([], A));

    [Fact]
    public void FindCycleThrough_SelfLoop_ReturnsPath() =>
        Assert.Equal([A, A], PrerequisiteCycleCheck.FindCycleThrough([(A, A)], A));

    [Fact]
    public void FindCycleThrough_DirectPair_ReturnsPathFromCourse() =>
        Assert.Equal([B, A, B], PrerequisiteCycleCheck.FindCycleThrough([(A, B), (B, A)], B));

    [Fact]
    public void FindCycleThrough_Indirect_ReturnsPathFromCourse() =>
        // Seed shape: CSC 310 → CSC 210 → CSC 101, then CSC 101 is made to require CSC 310.
        Assert.Equal([A, C, B, A], PrerequisiteCycleCheck.FindCycleThrough([(C, B), (B, A), (A, C)], A));

    [Fact]
    public void FindCycleThrough_Diamond_ReturnsNull() =>
        Assert.Null(PrerequisiteCycleCheck.FindCycleThrough([(A, B), (A, C), (B, D), (C, D)], A));

    [Fact]
    public void FindCycleThrough_CycleNotThroughCourse_ReturnsNull() =>
        Assert.Null(PrerequisiteCycleCheck.FindCycleThrough([(A, B), (B, C), (C, B)], A));
}
