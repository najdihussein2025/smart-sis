using SmartSis.Api.Features.Career.Models.Dtos;
using SmartSis.Api.Features.Career.Models.Entities;
using SmartSis.Api.Features.Career.Rules;

namespace SmartSis.Api.Features.Career.Interfaces;

/// <summary>Admin: traits, question bank, careers with roadmaps, course-trait tags (PB-16, PB-17, PB-20).</summary>
public interface ICareerAdminService
{
    Task<IReadOnlyList<TraitDto>> ListTraitsAsync(CancellationToken ct);
    Task<TraitDto> CreateTraitAsync(SaveTraitRequest request, CancellationToken ct);
    Task<TraitDto> UpdateTraitAsync(int id, SaveTraitRequest request, CancellationToken ct);

    Task<QuestionBankDto> ListQuestionsAsync(CancellationToken ct);
    Task<QuestionDto> GetQuestionAsync(int id, CancellationToken ct);
    Task<QuestionDto> CreateQuestionAsync(SaveQuestionRequest request, CancellationToken ct);
    Task<QuestionDto> UpdateQuestionAsync(int id, SaveQuestionRequest request, CancellationToken ct);
    Task DeleteQuestionAsync(int id, CancellationToken ct);

    Task<IReadOnlyList<CareerPathDto>> ListCareersAsync(CancellationToken ct);
    Task<CareerPathDto> GetCareerAsync(int id, CancellationToken ct);
    Task<CareerPathDto> CreateCareerAsync(SaveCareerPathRequest request, CancellationToken ct);
    Task<CareerPathDto> UpdateCareerAsync(int id, SaveCareerPathRequest request, CancellationToken ct);

    Task<IReadOnlyList<CourseTagsDto>> ListCourseTagsAsync(CancellationToken ct);
    Task<CourseTagsDto> SaveCourseTagsAsync(int courseId, SaveCourseTagsRequest request, CancellationToken ct);
}

/// <summary>Student: assessment, recommendations and roadmaps. The student always comes from the token.</summary>
public interface ICareerService
{
    Task<MyAssessmentsDto> GetMyAssessmentsAsync(CancellationToken ct);
    Task<QuizDto> GetQuizAsync(CancellationToken ct);
    Task<AssessmentDto> SubmitAsync(SubmitAssessmentRequest request, CancellationToken ct);
    Task<RecommendationsDto> GetRecommendationsAsync(CancellationToken ct);
    Task<IReadOnlyList<CareerSummaryDto>> ListCareersAsync(CancellationToken ct);
    Task<RoadmapDto> GetRoadmapAsync(int careerId, CancellationToken ct);
}

/// <summary>Writes the recommendation explanation with the AI service. Returns null when it can't (never throws).</summary>
public interface ICareerAiClient
{
    Task<string?> ExplainAsync(CareerExplainRequest request, CancellationToken ct);
}

/// <summary>What the AI service gets to write the explanation. No name, id or other personal data (minimal PII).</summary>
public sealed record CareerExplainRequest(
    IReadOnlyList<CareerExplainCareer> Careers,
    IReadOnlyList<CareerExplainTrait> Traits,
    IReadOnlyList<string> CompletedCourses,
    bool UsesGrades);

public sealed record CareerExplainCareer(string Name, int MatchPct, int QuizPct, int? GradePct, IReadOnlyList<string> TopTraits);

public sealed record CareerExplainTrait(string Name, int Score);

/// <summary>A section of the current term with seats and the student's status.</summary>
public sealed record SectionOffer(int SectionId, int CourseId, string SectionCode, int SeatsLeft, string Status);

/// <summary>A course row used by student views.</summary>
public sealed record CourseInfo(int Id, string Code, string Name, int Credits);

public interface ICareerRepository
{
    // Shared
    Task<(int Id, string Name)?> GetCurrentTermAsync(CancellationToken ct);
    Task<HashSet<int>> ExistingTraitIdsAsync(IEnumerable<int> ids, CancellationToken ct);
    Task<HashSet<int>> ExistingCourseIdsAsync(IEnumerable<int> ids, CancellationToken ct);

    // Traits
    Task<List<TraitDto>> ListTraitsAsync(CancellationToken ct);
    Task<CareerTrait?> FindTraitAsync(int id, CancellationToken ct);
    Task<bool> TraitNameExistsAsync(string name, int? excludeId, CancellationToken ct);
    void Add(object entity);
    void Remove(object entity);

    // Question bank
    Task<List<QuestionDto>> ListQuestionsAsync(int? id, CancellationToken ct);
    Task<CareerQuestion?> GetQuestionForUpdateAsync(int id, CancellationToken ct);
    Task<int> CountAnswersAsync(int questionId, CancellationToken ct);
    Task<HashSet<int>> OptionIdsWithAnswersAsync(IEnumerable<int> optionIds, CancellationToken ct);
    Task<List<CareerQuestion>> ListActiveQuestionsAsync(CancellationToken ct);

    // Careers
    Task<List<CareerPathDto>> ListCareersAsync(int? id, CancellationToken ct);
    Task<CareerPath?> GetCareerForUpdateAsync(int id, CancellationToken ct);
    Task<bool> CareerNameExistsAsync(string name, int? excludeId, CancellationToken ct);
    Task<List<CareerProfile>> ActiveCareerProfilesAsync(CancellationToken ct);
    Task<List<CareerSummaryDto>> ActiveCareersAsync(CancellationToken ct);
    Task<CareerPath?> GetActiveCareerWithStepsAsync(int id, CancellationToken ct);
    Task<List<(int CareerId, int CourseId, int StepOrder)>> RoadmapCoursesAsync(IEnumerable<int> careerIds, CancellationToken ct);

    // Course tags
    Task<List<CourseTagsDto>> ListCourseTagsAsync(int? courseId, CancellationToken ct);
    Task<List<CourseTrait>> GetCourseTagsForUpdateAsync(int courseId, CancellationToken ct);
    Task<Dictionary<int, IReadOnlyCollection<int>>> CourseTraitMapAsync(CancellationToken ct);

    // Student
    Task<List<AssessmentDto>> ListAssessmentsAsync(int studentId, CancellationToken ct);
    Task<bool> HasAssessmentInTermAsync(int studentId, int termId, CancellationToken ct);
    Task<List<CourseGrade>> CompletedGradesAsync(int studentId, CancellationToken ct);
    Task<HashSet<int>> PassedCourseIdsAsync(int studentId, CancellationToken ct);
    Task<HashSet<int>> EnrolledCourseIdsAsync(int studentId, int termId, CancellationToken ct);
    Task<List<string>> PassedCourseLabelsAsync(int studentId, CancellationToken ct);
    Task<List<CourseInfo>> CoursesAsync(IEnumerable<int> courseIds, CancellationToken ct);
    Task<List<SectionOffer>> SectionOffersAsync(int termId, IEnumerable<int> courseIds, int studentId, CancellationToken ct);
}
