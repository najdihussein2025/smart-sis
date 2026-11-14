using SmartSis.Api.Features.Career.Interfaces;
using SmartSis.Api.Features.Career.Models.Dtos;
using SmartSis.Api.Features.Career.Models.Entities;
using SmartSis.Api.Features.Career.Rules;
using SmartSis.Api.Shared.Auth;
using SmartSis.Api.Shared.Data;
using SmartSis.Api.Shared.Exceptions;

namespace SmartSis.Api.Features.Career.Services;

/// <summary>
/// Student side of the career module (PB-16, PB-17, PB-20). The student is always the signed-in user, so a student
/// only ever sees their own assessments. One assessment per term: a retake opens when the next term becomes current.
/// </summary>
public sealed class CareerService(
    ICareerRepository repo, ICareerAiClient ai, ICurrentUser me, IUnitOfWork uow, TimeProvider clock) : ICareerService
{
    public const string NoCurrentTerm = "no_current_term";
    public const string AlreadyTaken = "already_taken";
    public const string AssessmentRequired = "assessment_required";

    /// <summary>Suggested courses shown with the recommendations.</summary>
    public const int MaxSuggestedCourses = 6;

    public async Task<MyAssessmentsDto> GetMyAssessmentsAsync(CancellationToken ct)
    {
        var term = await repo.GetCurrentTermAsync(ct);
        var attempts = await repo.ListAssessmentsAsync(me.UserId, ct);
        string? reason = term is null
            ? "There is no current term, so the assessment is closed for now."
            : attempts.Any(a => a.TermId == term.Value.Id)
                ? $"You already took the assessment in {term.Value.Name}. You can retake it next term."
                : null;
        return new MyAssessmentsDto(term?.Id, term?.Name, reason is null, reason, attempts);
    }

    public async Task<QuizDto> GetQuizAsync(CancellationToken ct)
    {
        var term = await EnsureCanTakeAsync(ct);
        var quiz = AssessmentScoring.PickQuiz(await repo.ListActiveQuestionsAsync(ct), q => q.Id);
        return new QuizDto(term.Name, quiz.Select(q => new QuizQuestionDto(
            q.Id, q.Text,
            q.Options.OrderBy(o => o.OptionOrder).ThenBy(o => o.Id).Select(o => new QuizOptionDto(o.Id, o.Text)).ToList())).ToList());
    }

    public async Task<AssessmentDto> SubmitAsync(SubmitAssessmentRequest request, CancellationToken ct)
    {
        var term = await EnsureCanTakeAsync(ct);
        var quiz = AssessmentScoring.PickQuiz(await repo.ListActiveQuestionsAsync(ct), q => q.Id)
            .Select(q => new QuizQuestion(q.Id, q.Options
                .Select(o => new QuizOption(o.Id, o.TraitPoints.ToDictionary(p => p.TraitId, p => p.Points)))
                .ToList()))
            .ToList();

        var answers = AssessmentScoring.CheckAnswers(quiz, request.Answers.Select(a => new QuizAnswer(a.QuestionId, a.OptionId)).ToList());
        var scores = AssessmentScoring.Score(quiz, answers);

        var assessment = new CareerAssessment
        {
            StudentId = me.UserId,
            TermId = term.Id,
            SubmittedAt = clock.GetUtcNow().UtcDateTime,
        };
        foreach (var (questionId, optionId) in answers)
            assessment.Answers.Add(new CareerAssessmentAnswer { QuestionId = questionId, OptionId = optionId });
        foreach (var s in scores)
            assessment.TraitScores.Add(new CareerAssessmentTraitScore { TraitId = s.TraitId, Points = s.Points, MaxPoints = s.MaxPoints, Score = s.Score });

        repo.Add(assessment);
        await uow.SaveChangesAsync(ct);
        return (await repo.ListAssessmentsAsync(me.UserId, ct)).First(a => a.Id == assessment.Id);
    }

    public async Task<RecommendationsDto> GetRecommendationsAsync(CancellationToken ct)
    {
        var studentId = me.UserId;
        var latest = (await repo.ListAssessmentsAsync(studentId, ct)).FirstOrDefault()
                     ?? throw new BusinessRuleException("Take the career assessment first to see your recommendations.", AssessmentRequired);

        var quiz = latest.Traits.ToDictionary(t => t.TraitId, t => (decimal)t.Score);
        var grades = CareerScoring.GradeTraitScores(await repo.CompletedGradesAsync(studentId, ct), await repo.CourseTraitMapAsync(ct));
        var top = CareerScoring.TopMatches(await repo.ActiveCareerProfilesAsync(ct), quiz, grades);

        var traitNames = (await repo.ListTraitsAsync(ct)).ToDictionary(t => t.Id, t => t.Name);
        var descriptions = (await repo.ActiveCareersAsync(ct)).ToDictionary(c => c.Id, c => c.Description);
        var careers = top.Select(m => new CareerMatchDto(
            m.CareerId, m.Name, descriptions.GetValueOrDefault(m.CareerId), m.MatchPct, m.QuizPct, m.GradePct,
            m.TopTraitIds.Select(id => traitNames.GetValueOrDefault(id, "")).Where(n => n != "").ToList())).ToList();
        var usesGrades = top.Any(m => m.GradePct is not null);

        var (explanation, source) = await ExplainAsync(careers, latest, usesGrades, studentId, ct);
        var suggested = await SuggestCoursesAsync(careers, studentId, ct);

        return new RecommendationsDto(latest.Id, latest.TermName, latest.SubmittedAt, usesGrades, careers, explanation, source, suggested);
    }

    public async Task<IReadOnlyList<CareerSummaryDto>> ListCareersAsync(CancellationToken ct) => await repo.ActiveCareersAsync(ct);

    public async Task<RoadmapDto> GetRoadmapAsync(int careerId, CancellationToken ct)
    {
        var studentId = me.UserId;
        var career = await repo.GetActiveCareerWithStepsAsync(careerId, ct) ?? throw NotFoundException.For<CareerPath>(careerId);
        var steps = career.Steps.OrderBy(s => s.StepOrder).ToList();

        var term = await repo.GetCurrentTermAsync(ct);
        var passed = await repo.PassedCourseIdsAsync(studentId, ct);
        var enrolled = term is null ? [] : await repo.EnrolledCourseIdsAsync(studentId, term.Value.Id, ct);
        var statuses = RoadmapProgress.Statuses(steps.Select(s => s.CourseId).ToList(), passed, enrolled);

        var courseIds = steps.Where(s => s.CourseId is not null).Select(s => s.CourseId!.Value).ToList();
        var sections = await SectionsByCourseAsync(term?.Id, courseIds, studentId, ct);

        var views = steps.Select((s, i) => new RoadmapStepViewDto(
            s.StepOrder, s.Title, s.Description, statuses[i],
            s.Course is null ? null : new RoadmapCourseDto(
                s.Course.Id, s.Course.Code, s.Course.Name, s.Course.Credits, sections.GetValueOrDefault(s.Course.Id, [])))).ToList();

        return new RoadmapDto(career.Id, career.Name, career.Description,
            statuses.Count(x => x == RoadmapStepStatus.Done), courseIds.Count, views);
    }

    private async Task<(int Id, string Name)> EnsureCanTakeAsync(CancellationToken ct)
    {
        var term = await repo.GetCurrentTermAsync(ct)
                   ?? throw new BusinessRuleException("There is no current term, so the assessment is closed for now.", NoCurrentTerm);
        if (await repo.HasAssessmentInTermAsync(me.UserId, term.Id, ct))
            throw new BusinessRuleException(
                $"You already took the assessment in {term.Name}. You can retake it next term.", AlreadyTaken);
        return term;
    }

    /// <summary>The LLM writes the explanation; if the AI service can't, the built-in text is used (never fails).</summary>
    private async Task<(string Text, ExplanationSource Source)> ExplainAsync(
        IReadOnlyList<CareerMatchDto> careers, AssessmentDto latest, bool usesGrades, int studentId, CancellationToken ct)
    {
        if (careers.Count > 0)
        {
            var request = new CareerExplainRequest(
                careers.Select(c => new CareerExplainCareer(c.Name, c.MatchPct, c.QuizPct, c.GradePct, c.TopTraits)).ToList(),
                latest.Traits.Select(t => new CareerExplainTrait(t.TraitName, t.Score)).ToList(),
                await repo.PassedCourseLabelsAsync(studentId, ct),
                usesGrades);
            var text = await ai.ExplainAsync(request, ct);
            if (!string.IsNullOrWhiteSpace(text)) return (text.Trim(), ExplanationSource.Ai);
        }

        var fallback = CareerExplanation.Fallback(
            careers.Select(c => new ExplainedCareer(c.Name, c.MatchPct, c.TopTraits)).ToList(), usesGrades);
        return (fallback, ExplanationSource.Fallback);
    }

    /// <summary>Roadmap courses of the recommended careers that the student hasn't passed and isn't taking, best career first.</summary>
    private async Task<IReadOnlyList<SuggestedCourseDto>> SuggestCoursesAsync(
        IReadOnlyList<CareerMatchDto> careers, int studentId, CancellationToken ct)
    {
        if (careers.Count == 0) return [];

        var term = await repo.GetCurrentTermAsync(ct);
        var passed = await repo.PassedCourseIdsAsync(studentId, ct);
        var enrolled = term is null ? [] : await repo.EnrolledCourseIdsAsync(studentId, term.Value.Id, ct);
        var rank = careers.Select((c, i) => (c.CareerId, i)).ToDictionary(x => x.CareerId, x => x.i);
        var names = careers.ToDictionary(c => c.CareerId, c => c.Name);

        var steps = (await repo.RoadmapCoursesAsync(rank.Keys, ct))
            .Where(s => !passed.Contains(s.CourseId) && !enrolled.Contains(s.CourseId))
            .ToList();
        var ordered = steps
            .GroupBy(s => s.CourseId)
            .Select(g => (CourseId: g.Key,
                          Best: g.Min(s => (rank[s.CareerId], s.StepOrder)),
                          Careers: g.OrderBy(s => rank[s.CareerId]).Select(s => names[s.CareerId]).Distinct().ToList()))
            .OrderBy(x => x.Best)
            .Take(MaxSuggestedCourses)
            .ToList();
        if (ordered.Count == 0) return [];

        var courses = (await repo.CoursesAsync(ordered.Select(x => x.CourseId), ct)).ToDictionary(c => c.Id);
        var sections = await SectionsByCourseAsync(term?.Id, ordered.Select(x => x.CourseId), studentId, ct);
        return ordered.Where(x => courses.ContainsKey(x.CourseId)).Select(x =>
        {
            var c = courses[x.CourseId];
            return new SuggestedCourseDto(c.Id, c.Code, c.Name, c.Credits, x.Careers, sections.GetValueOrDefault(c.Id, []));
        }).ToList();
    }

    private async Task<Dictionary<int, IReadOnlyList<CareerSectionDto>>> SectionsByCourseAsync(
        int? termId, IEnumerable<int> courseIds, int studentId, CancellationToken ct)
    {
        if (termId is null) return [];
        var offers = await repo.SectionOffersAsync(termId.Value, courseIds, studentId, ct);
        return offers.GroupBy(o => o.CourseId).ToDictionary(
            g => g.Key,
            g => (IReadOnlyList<CareerSectionDto>)g.Select(o => new CareerSectionDto(o.SectionId, o.SectionCode, o.SeatsLeft, o.Status)).ToList());
    }
}
