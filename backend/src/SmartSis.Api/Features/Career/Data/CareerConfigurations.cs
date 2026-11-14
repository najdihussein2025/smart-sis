using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSis.Api.Features.Career.Models.Entities;

namespace SmartSis.Api.Features.Career.Data;

internal sealed class CareerTraitConfiguration : IEntityTypeConfiguration<CareerTrait>
{
    public void Configure(EntityTypeBuilder<CareerTrait> b)
    {
        b.ToTable("CareerTraits");
        b.Property(x => x.Name).HasMaxLength(60);
        b.Property(x => x.Description).HasMaxLength(300);
        b.HasIndex(x => x.Name).IsUnique();
    }
}

internal sealed class CareerPathConfiguration : IEntityTypeConfiguration<CareerPath>
{
    public void Configure(EntityTypeBuilder<CareerPath> b)
    {
        b.ToTable("CareerPaths");
        b.Property(x => x.Name).HasMaxLength(100);
        b.Property(x => x.Description).HasMaxLength(1000);
        b.HasIndex(x => x.Name).IsUnique();
    }
}

internal sealed class CareerPathTraitConfiguration : IEntityTypeConfiguration<CareerPathTrait>
{
    public void Configure(EntityTypeBuilder<CareerPathTrait> b)
    {
        b.ToTable("CareerPathTraits", t => t.HasCheckConstraint("CK_CareerPathTraits_Weight", "[Weight] BETWEEN 1 AND 10"));
        b.HasIndex(x => new { x.CareerPathId, x.TraitId }).IsUnique();
        b.HasOne(x => x.CareerPath).WithMany(c => c.Traits).HasForeignKey(x => x.CareerPathId);
        b.HasOne(x => x.Trait).WithMany().HasForeignKey(x => x.TraitId);
    }
}

internal sealed class CareerRoadmapStepConfiguration : IEntityTypeConfiguration<CareerRoadmapStep>
{
    public void Configure(EntityTypeBuilder<CareerRoadmapStep> b)
    {
        b.ToTable("CareerRoadmapSteps", t => t.HasCheckConstraint("CK_CareerRoadmapSteps_Order", "[StepOrder] >= 1"));
        b.Property(x => x.Title).HasMaxLength(150);
        b.Property(x => x.Description).HasMaxLength(1000);
        b.HasIndex(x => new { x.CareerPathId, x.StepOrder }).IsUnique();
        b.HasOne(x => x.CareerPath).WithMany(c => c.Steps).HasForeignKey(x => x.CareerPathId);
        b.HasOne(x => x.Course).WithMany().HasForeignKey(x => x.CourseId).IsRequired(false);
    }
}

internal sealed class CourseTraitConfiguration : IEntityTypeConfiguration<CourseTrait>
{
    public void Configure(EntityTypeBuilder<CourseTrait> b)
    {
        b.ToTable("CourseTraits");
        b.HasIndex(x => new { x.CourseId, x.TraitId }).IsUnique();
        b.HasOne(x => x.Course).WithMany().HasForeignKey(x => x.CourseId);
        b.HasOne(x => x.Trait).WithMany().HasForeignKey(x => x.TraitId);
    }
}

internal sealed class CareerQuestionConfiguration : IEntityTypeConfiguration<CareerQuestion>
{
    public void Configure(EntityTypeBuilder<CareerQuestion> b)
    {
        b.ToTable("CareerQuestions");
        b.Property(x => x.Text).HasMaxLength(500);
        b.HasIndex(x => x.IsActive);
    }
}

internal sealed class CareerQuestionOptionConfiguration : IEntityTypeConfiguration<CareerQuestionOption>
{
    public void Configure(EntityTypeBuilder<CareerQuestionOption> b)
    {
        b.ToTable("CareerQuestionOptions");
        b.Property(x => x.Text).HasMaxLength(300);
        b.HasIndex(x => new { x.QuestionId, x.OptionOrder });
        b.HasOne(x => x.Question).WithMany(q => q.Options).HasForeignKey(x => x.QuestionId);
    }
}

internal sealed class CareerOptionTraitPointConfiguration : IEntityTypeConfiguration<CareerOptionTraitPoint>
{
    public void Configure(EntityTypeBuilder<CareerOptionTraitPoint> b)
    {
        b.ToTable("CareerOptionTraitPoints", t => t.HasCheckConstraint("CK_CareerOptionTraitPoints_Points", "[Points] BETWEEN 1 AND 5"));
        b.HasIndex(x => new { x.OptionId, x.TraitId }).IsUnique();
        b.HasOne(x => x.Option).WithMany(o => o.TraitPoints).HasForeignKey(x => x.OptionId);
        b.HasOne(x => x.Trait).WithMany().HasForeignKey(x => x.TraitId);
    }
}

internal sealed class CareerAssessmentConfiguration : IEntityTypeConfiguration<CareerAssessment>
{
    public void Configure(EntityTypeBuilder<CareerAssessment> b)
    {
        b.ToTable("CareerAssessments");
        // One assessment per student per term (retake next term).
        b.HasIndex(x => new { x.StudentId, x.TermId }).IsUnique();
        b.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId);
        b.HasOne(x => x.Term).WithMany().HasForeignKey(x => x.TermId);
    }
}

internal sealed class CareerAssessmentAnswerConfiguration : IEntityTypeConfiguration<CareerAssessmentAnswer>
{
    public void Configure(EntityTypeBuilder<CareerAssessmentAnswer> b)
    {
        b.ToTable("CareerAssessmentAnswers");
        // Each quiz question answered exactly once.
        b.HasIndex(x => new { x.AssessmentId, x.QuestionId }).IsUnique();
        b.HasOne(x => x.Assessment).WithMany(a => a.Answers).HasForeignKey(x => x.AssessmentId);
        b.HasOne(x => x.Question).WithMany().HasForeignKey(x => x.QuestionId);
        b.HasOne(x => x.Option).WithMany().HasForeignKey(x => x.OptionId);
    }
}

internal sealed class CareerAssessmentTraitScoreConfiguration : IEntityTypeConfiguration<CareerAssessmentTraitScore>
{
    public void Configure(EntityTypeBuilder<CareerAssessmentTraitScore> b)
    {
        b.ToTable("CareerAssessmentTraitScores", t => t.HasCheckConstraint("CK_CareerAssessmentTraitScores_Score", "[Score] BETWEEN 0 AND 100"));
        b.HasIndex(x => new { x.AssessmentId, x.TraitId }).IsUnique();
        b.HasOne(x => x.Assessment).WithMany(a => a.TraitScores).HasForeignKey(x => x.AssessmentId);
        b.HasOne(x => x.Trait).WithMany().HasForeignKey(x => x.TraitId);
    }
}
