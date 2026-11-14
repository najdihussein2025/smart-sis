using SmartSis.Api.Features.Courses.Models.Entities;
using SmartSis.Api.Features.Terms.Models.Entities;
using SmartSis.Api.Features.Users.Models.Entities;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Career.Models.Entities;

/// <summary>A trait or skill the quiz measures and careers weigh (e.g. "Analytical thinking").</summary>
public class CareerTrait : BaseEntity
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
}

/// <summary>A career path students can be matched to (PB-17), with trait weights and a roadmap (PB-20).</summary>
public class CareerPath : BaseEntity
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }

    /// <summary>Inactive careers are hidden from students and never recommended.</summary>
    public bool IsActive { get; set; } = true;

    public ICollection<CareerPathTrait> Traits { get; set; } = [];
    public ICollection<CareerRoadmapStep> Steps { get; set; } = [];
}

/// <summary>How much a trait matters for a career (1–10).</summary>
public class CareerPathTrait : BaseEntity
{
    public int CareerPathId { get; set; }
    public CareerPath CareerPath { get; set; } = null!;

    public int TraitId { get; set; }
    public CareerTrait Trait { get; set; } = null!;

    public int Weight { get; set; }
}

/// <summary>One ordered step of a career's roadmap, optionally linked to a course.</summary>
public class CareerRoadmapStep : BaseEntity
{
    public int CareerPathId { get; set; }
    public CareerPath CareerPath { get; set; } = null!;

    /// <summary>1-based position in the roadmap.</summary>
    public int StepOrder { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }

    public int? CourseId { get; set; }
    public Course? Course { get; set; }
}

/// <summary>Tags a course with a trait: grades in it count towards that trait (~30% of the match).</summary>
public class CourseTrait : BaseEntity
{
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public int TraitId { get; set; }
    public CareerTrait Trait { get; set; } = null!;
}

/// <summary>A question in the career assessment bank (PB-16).</summary>
public class CareerQuestion : BaseEntity
{
    public string Text { get; set; } = "";

    /// <summary>Only active questions go into the quiz. Answered questions are deactivated, never deleted.</summary>
    public bool IsActive { get; set; } = true;

    public ICollection<CareerQuestionOption> Options { get; set; } = [];
}

/// <summary>One answer option. Choosing it adds points to traits.</summary>
public class CareerQuestionOption : BaseEntity
{
    public int QuestionId { get; set; }
    public CareerQuestion Question { get; set; } = null!;

    /// <summary>1-based position under the question.</summary>
    public int OptionOrder { get; set; }
    public string Text { get; set; } = "";

    public ICollection<CareerOptionTraitPoint> TraitPoints { get; set; } = [];
}

/// <summary>Points an option adds to a trait (1–5).</summary>
public class CareerOptionTraitPoint : BaseEntity
{
    public int OptionId { get; set; }
    public CareerQuestionOption Option { get; set; } = null!;

    public int TraitId { get; set; }
    public CareerTrait Trait { get; set; } = null!;

    public int Points { get; set; }
}

/// <summary>A submitted assessment: one per student per term (unique index).</summary>
public class CareerAssessment : BaseEntity
{
    /// <summary>Students.UserId.</summary>
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public int TermId { get; set; }
    public Term Term { get; set; } = null!;

    public DateTime SubmittedAt { get; set; }

    public ICollection<CareerAssessmentAnswer> Answers { get; set; } = [];
    public ICollection<CareerAssessmentTraitScore> TraitScores { get; set; } = [];
}

/// <summary>The option chosen for one question of an assessment.</summary>
public class CareerAssessmentAnswer : BaseEntity
{
    public int AssessmentId { get; set; }
    public CareerAssessment Assessment { get; set; } = null!;

    public int QuestionId { get; set; }
    public CareerQuestion Question { get; set; } = null!;

    public int OptionId { get; set; }
    public CareerQuestionOption Option { get; set; } = null!;
}

/// <summary>Quiz result per trait, stored at submit so later bank edits don't change history.</summary>
public class CareerAssessmentTraitScore : BaseEntity
{
    public int AssessmentId { get; set; }
    public CareerAssessment Assessment { get; set; } = null!;

    public int TraitId { get; set; }
    public CareerTrait Trait { get; set; } = null!;

    /// <summary>Points earned for the trait.</summary>
    public int Points { get; set; }

    /// <summary>Points the quiz could give the trait at most.</summary>
    public int MaxPoints { get; set; }

    /// <summary>Points / MaxPoints as a whole percentage (0–100).</summary>
    public int Score { get; set; }
}
