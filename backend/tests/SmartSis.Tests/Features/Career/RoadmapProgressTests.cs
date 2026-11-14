using SmartSis.Api.Features.Career.Rules;

namespace SmartSis.Tests.Features.Career;

/// <summary>PB-20 / T72, T74: roadmap steps done / in progress / next.</summary>
public sealed class RoadmapProgressTests
{
    [Fact]
    public void Statuses_DoneInProgressNextLaterMilestone()
    {
        var statuses = RoadmapProgress.Statuses(
            [101, 210, null, 201, 310, 999],
            passedCourseIds: new HashSet<int> { 101 },
            enrolledCourseIds: new HashSet<int> { 210 });

        Assert.Equal(
            [RoadmapStepStatus.Done, RoadmapStepStatus.InProgress, RoadmapStepStatus.Milestone,
             RoadmapStepStatus.Next, RoadmapStepStatus.Later, RoadmapStepStatus.Later],
            statuses);
    }

    [Fact]
    public void Statuses_AllPassed_NoNext() =>
        Assert.All(RoadmapProgress.Statuses([1, 2], new HashSet<int> { 1, 2 }, new HashSet<int>()),
            s => Assert.Equal(RoadmapStepStatus.Done, s));

    [Fact]
    public void Statuses_PassedWinsOverEnrolled() =>
        Assert.Equal(RoadmapStepStatus.Done, RoadmapProgress.Statuses([1], new HashSet<int> { 1 }, new HashSet<int> { 1 })[0]);

    [Fact]
    public void Statuses_Empty() => Assert.Empty(RoadmapProgress.Statuses([], new HashSet<int>(), new HashSet<int>()));
}

/// <summary>PB-17 / T66: the built-in explanation used when the AI service can't answer.</summary>
public sealed class CareerExplanationTests
{
    [Fact]
    public void Fallback_NamesTheBestCareerTraitsAndTheOthers()
    {
        var text = CareerExplanation.Fallback(
            [new("Data Scientist", 81, ["Analytical thinking", "Attention to detail"]), new("Software Engineer", 77, []), new("UX Designer", 60, [])],
            hasGrades: true);

        Assert.StartsWith("Your strongest match is Data Scientist (81%), driven mostly by Analytical thinking and Attention to detail.", text);
        Assert.Contains("Software Engineer (77%) and UX Designer (60%)", text);
        Assert.Contains("grades in related courses (about 30%)", text);
    }

    [Fact]
    public void Fallback_NoGrades_SaysSo() =>
        Assert.Contains("will refine them", CareerExplanation.Fallback([new("Data Scientist", 81, [])], hasGrades: false));

    [Fact]
    public void Fallback_NoCareers_Explains() =>
        Assert.Contains("no career paths are set up", CareerExplanation.Fallback([], hasGrades: false));
}
