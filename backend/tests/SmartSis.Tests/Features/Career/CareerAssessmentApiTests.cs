using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Career.Models.Dtos;
using SmartSis.Api.Features.Career.Models.Entities;
using SmartSis.Api.Features.Terms.Models.Entities;
using SmartSis.Tests.Infrastructure;
using static SmartSis.Tests.Features.Career.CareerTestSupport;

namespace SmartSis.Tests.Features.Career;

/// <summary>
/// PB-16 / T27, T28, T30: the student quiz on the seeded bank (24 active questions). Each test that submits uses its own
/// new student, because a student can take the assessment once per term.
/// </summary>
public sealed class CareerAssessmentApiTests(SisApiFactory factory) : IClassFixture<SisApiFactory>
{
    private static long _next = 3900;
    private static long NextId() => Interlocked.Increment(ref _next);

    [Fact]
    public async Task Quiz_ReturnsActiveQuestions_WithoutTraitPoints()
    {
        var student = await NewStudentAsync(factory, NextId());
        var res = await student.GetAsync("/api/career/quiz");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var json = await res.Content.ReadAsStringAsync();
        Assert.DoesNotContain("points", json, StringComparison.OrdinalIgnoreCase);
        var quiz = await res.ReadAsync<QuizDto>();
        Assert.Equal(24, quiz.Questions.Count);
        Assert.All(quiz.Questions, q => Assert.Equal(4, q.Options.Count));
        Assert.Equal(quiz.Questions.OrderBy(q => q.Id).Select(q => q.Id), quiz.Questions.Select(q => q.Id));
    }

    [Fact]
    public async Task Submit_AllAnswered_SavesTraitScores_ThenLockedForTheTerm()
    {
        var student = await NewStudentAsync(factory, NextId());
        var (_, answers) = await AnswerAllAsync(student);

        var res = await SubmitAsync(student, answers);

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var assessment = await res.ReadAsync<AssessmentDto>();
        Assert.NotEmpty(assessment.Traits);
        Assert.All(assessment.Traits, t => Assert.InRange(t.Score, 0, 100));
        Assert.Equal(assessment.Traits.OrderByDescending(t => t.Score).ThenBy(t => t.TraitName).Select(t => t.TraitId),
            assessment.Traits.Select(t => t.TraitId));

        var mine = await (await student.GetAsync("/api/career/assessments")).ReadAsync<MyAssessmentsDto>();
        Assert.False(mine.CanTake);
        Assert.Contains("retake it next term", mine.Reason);
        Assert.Equal(assessment.Id, Assert.Single(mine.Attempts).Id);

        // Attempt limit: once per term (T28).
        var again = await SubmitAsync(student, answers);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal("already_taken", (await again.ReadAsync<ProblemDetails>()).Extensions["code"]!.ToString());
        Assert.Equal(HttpStatusCode.Conflict, (await student.GetAsync("/api/career/quiz")).StatusCode);
    }

    [Fact]
    public async Task Submit_StoresOneAnswerPerQuestion()
    {
        var student = await NewStudentAsync(factory, NextId());
        var (quiz, answers) = await AnswerAllAsync(student, optionIndex: 2);
        var id = (await (await SubmitAsync(student, answers)).ReadAsync<AssessmentDto>()).Id;

        await factory.WithDbAsync(async db =>
        {
            var saved = await db.Set<CareerAssessmentAnswer>().Where(a => a.AssessmentId == id).ToListAsync();
            Assert.Equal(quiz.Questions.Count, saved.Count);
            Assert.Equal(answers.Select(a => (a.QuestionId, a.OptionId)).Order(), saved.Select(a => (a.QuestionId, a.OptionId)).Order());
        });
    }

    [Fact]
    public async Task Submit_QuestionLeftOut_409()
    {
        var student = await NewStudentAsync(factory, NextId());
        var (_, answers) = await AnswerAllAsync(student);

        var res = await SubmitAsync(student, answers[1..]);

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        var problem = await res.ReadAsync<ProblemDetails>();
        Assert.Equal("quiz_incomplete", problem.Extensions["code"]!.ToString());
        Assert.Equal("Answer every question: 1 is still unanswered.", problem.Title);
    }

    [Fact]
    public async Task Submit_SameQuestionTwice_400()
    {
        var student = await NewStudentAsync(factory, NextId());
        var (_, answers) = await AnswerAllAsync(student);

        var res = await SubmitAsync(student, [.. answers, answers[0]]);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Contains($"answers[{answers.Length}].questionId", (await res.ReadAsync<ValidationProblemDetails>()).Errors.Keys);
    }

    [Fact]
    public async Task Submit_OptionOfAnotherQuestion_400()
    {
        var student = await NewStudentAsync(factory, NextId());
        var (quiz, answers) = await AnswerAllAsync(student);
        answers[1] = answers[1] with { OptionId = quiz.Questions[0].Options[0].Id };

        var res = await SubmitAsync(student, answers);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Contains("answers[1].optionId", (await res.ReadAsync<ValidationProblemDetails>()).Errors.Keys);
    }

    [Fact]
    public async Task Submit_QuestionNotInQuiz_409QuizChanged()
    {
        var student = await NewStudentAsync(factory, NextId());
        var (_, answers) = await AnswerAllAsync(student);

        var res = await SubmitAsync(student, [.. answers[1..], new AnswerRequest(999_999, 1)]);

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal("quiz_changed", (await res.ReadAsync<ProblemDetails>()).Extensions["code"]!.ToString());
    }

    [Fact]
    public async Task Submit_EmptyAnswers_400()
    {
        var student = await NewStudentAsync(factory, NextId());
        var res = await SubmitAsync(student, []);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Student_SeesOnlyOwnAttempts()
    {
        var first = await NewStudentAsync(factory, NextId());
        var (_, answers) = await AnswerAllAsync(first);
        Assert.Equal(HttpStatusCode.Created, (await SubmitAsync(first, answers)).StatusCode);

        var other = await NewStudentAsync(factory, NextId());
        var mine = await (await other.GetAsync("/api/career/assessments")).ReadAsync<MyAssessmentsDto>();

        Assert.True(mine.CanTake);
        Assert.Null(mine.Reason);
        Assert.Empty(mine.Attempts);
    }

    [Fact]
    public async Task Admin_CannotTakeTheQuiz()
    {
        var admin = await factory.ClientForAsync(SisApiFactory.AdminId);
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.GetAsync("/api/career/quiz")).StatusCode);
    }
}

/// <summary>
/// T28: the quiz is closed when the bank is too small or there is no current term. Own fixture, because these tests change
/// the shared bank and term.
/// </summary>
public sealed class CareerQuizAvailabilityTests(SisApiFactory factory) : IClassFixture<SisApiFactory>
{
    [Fact]
    public async Task TooFewActiveQuestions_409_ThenBackWhenReactivated()
    {
        var student = await factory.ClientForAsync(SisApiFactory.StudentId);
        List<int> deactivated = [];
        await factory.WithDbAsync(async db =>
        {
            var questions = await db.Set<CareerQuestion>().Where(q => q.IsActive).OrderBy(q => q.Id).Take(5).ToListAsync();
            questions.ForEach(q => q.IsActive = false);
            deactivated = questions.Select(q => q.Id).ToList();
            await db.SaveChangesAsync();
        });
        try
        {
            var res = await student.GetAsync("/api/career/quiz");
            Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
            Assert.Equal("not_enough_questions", (await res.ReadAsync<ProblemDetails>()).Extensions["code"]!.ToString());
        }
        finally
        {
            await factory.WithDbAsync(async db =>
            {
                foreach (var q in await db.Set<CareerQuestion>().Where(q => deactivated.Contains(q.Id)).ToListAsync()) q.IsActive = true;
                await db.SaveChangesAsync();
            });
        }

        var quiz = await (await student.GetAsync("/api/career/quiz")).ReadAsync<QuizDto>();
        Assert.Equal(24, quiz.Questions.Count);
    }

    [Fact]
    public async Task NoCurrentTerm_409_AndCannotTake()
    {
        var student = await factory.ClientForAsync(SisApiFactory.StudentId);
        await factory.WithDbAsync(async db =>
        {
            (await db.Set<Term>().SingleAsync(t => t.IsCurrent)).IsCurrent = false;
            await db.SaveChangesAsync();
        });
        try
        {
            var res = await student.GetAsync("/api/career/quiz");
            Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
            Assert.Equal("no_current_term", (await res.ReadAsync<ProblemDetails>()).Extensions["code"]!.ToString());

            var mine = await (await student.GetAsync("/api/career/assessments")).ReadAsync<MyAssessmentsDto>();
            Assert.False(mine.CanTake);
            Assert.Null(mine.CurrentTermId);
        }
        finally
        {
            await factory.WithDbAsync(async db =>
            {
                (await db.Set<Term>().OrderBy(t => t.Id).FirstAsync()).IsCurrent = true;
                await db.SaveChangesAsync();
            });
        }
    }
}
