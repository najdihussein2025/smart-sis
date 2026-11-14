namespace SmartSis.Api.Features.Career.Rules;

/// <summary>Where a roadmap step stands for the student.</summary>
public enum RoadmapStepStatus
{
    /// <summary>The step's course is passed (D or better).</summary>
    Done,
    /// <summary>The student is enrolled in the step's course this term.</summary>
    InProgress,
    /// <summary>The first course step that is neither done nor in progress.</summary>
    Next,
    /// <summary>A later course step.</summary>
    Later,
    /// <summary>A step without a course (e.g. "Build a portfolio"): advice, not tracked.</summary>
    Milestone,
}

/// <summary>PB-20 / T72: personalizes a career's roadmap from the student's record. Pure.</summary>
public static class RoadmapProgress
{
    /// <summary>Status per step, in roadmap order. <paramref name="stepCourseIds"/> holds null for steps without a course.</summary>
    public static IReadOnlyList<RoadmapStepStatus> Statuses(
        IReadOnlyList<int?> stepCourseIds, IReadOnlySet<int> passedCourseIds, IReadOnlySet<int> enrolledCourseIds)
    {
        var result = new List<RoadmapStepStatus>(stepCourseIds.Count);
        var nextGiven = false;
        foreach (var courseId in stepCourseIds)
        {
            if (courseId is not { } id) result.Add(RoadmapStepStatus.Milestone);
            else if (passedCourseIds.Contains(id)) result.Add(RoadmapStepStatus.Done);
            else if (enrolledCourseIds.Contains(id)) result.Add(RoadmapStepStatus.InProgress);
            else if (!nextGiven)
            {
                result.Add(RoadmapStepStatus.Next);
                nextGiven = true;
            }
            else result.Add(RoadmapStepStatus.Later);
        }
        return result;
    }
}
