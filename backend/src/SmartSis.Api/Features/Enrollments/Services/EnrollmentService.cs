using SmartSis.Api.Features.Cart.Interfaces;
using SmartSis.Api.Features.Enrollments.Enums;
using SmartSis.Api.Features.Enrollments.Interfaces;
using SmartSis.Api.Features.Enrollments.Models.Dtos;
using SmartSis.Api.Features.Enrollments.Models.Entities;
using SmartSis.Api.Features.Enrollments.Rules;
using SmartSis.Api.Features.Sections.Models.Entities;
using SmartSis.Api.Features.Terms.Enums;
using SmartSis.Api.Features.Terms.Models.Entities;
using SmartSis.Api.Shared.Auth;
using SmartSis.Api.Shared.Data;
using SmartSis.Api.Shared.Exceptions;

namespace SmartSis.Api.Features.Enrollments.Services;

/// <summary>
/// Every enroll / swap / drop runs in a transaction: lock the section row → gather the facts → policy → write the
/// enrollment and its EnrollmentActions row (rule E9) → take the section out of the cart.
/// </summary>
public sealed class EnrollmentService(
    IEnrollmentRepository enrollments,
    ICartService cart,
    ICurrentUser me,
    IUnitOfWork uow,
    TimeProvider clock) : IEnrollmentService
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    public async Task<MyEnrollmentsDto> GetMineAsync(CancellationToken ct)
    {
        var studentId = me.UserId;
        var max = await enrollments.GetMaxCreditsAsync(studentId, ct);
        var term = await enrollments.GetCurrentTermAsync(ct);
        if (term is null) return new MyEnrollmentsDto(null, null, TermWindow.Closed.ToString(), 0, max, []);

        var items = await enrollments.ListMineAsync(studentId, term.Id, ct);
        return new MyEnrollmentsDto(term.Id, term.Name, WindowOf(term).ToString(), items.Sum(i => i.Credits), max, items);
    }

    public async Task<EnrollResultDto> EnrollAsync(EnrollRequest request, CancellationToken ct)
    {
        var studentId = me.UserId;
        var results = new List<EnrollOutcomeDto>();

        // One transaction per section, in order: a later section sees the credits and meetings of the earlier ones.
        foreach (var sectionId in request.SectionIds)
        {
            string? label = null;
            try
            {
                await uow.ExecuteInTransactionAsync(async () =>
                {
                    var term = await enrollments.GetCurrentTermAsync(ct)
                               ?? throw new BusinessRuleException("There is no current term to enroll in.", EnrollmentPolicy.WindowClosed);
                    var section = await enrollments.LockSectionAsync(sectionId, ct) ?? throw NotFoundException.For<Section>(sectionId);
                    label = section.Candidate.Label;
                    if (section.TermId != term.Id)
                        throw new BusinessRuleException($"{label} isn't offered in the current term.", "not_current_term");

                    var state = await StateAsync(studentId, term, ct);
                    EnrollmentPolicy.CheckEnroll(state, section.Candidate);

                    enrollments.Add(new Enrollment { StudentId = studentId, SectionId = sectionId, EnrolledAt = Now });
                    enrollments.Add(Action(studentId, EnrollmentActionType.Enroll, from: null, to: sectionId));
                    await cart.RemoveIfPresentAsync(studentId, sectionId, ct);
                }, ct);
                results.Add(new EnrollOutcomeDto(sectionId, label, true, null, null));
            }
            catch (BusinessRuleException ex)
            {
                results.Add(new EnrollOutcomeDto(sectionId, label, false, ex.Message, ex.Code));
            }
            catch (NotFoundException)
            {
                results.Add(new EnrollOutcomeDto(sectionId, null, false, "This section doesn't exist.", "not_found"));
            }
        }

        return new EnrollResultDto(results, results.Count(r => r.Success));
    }

    public async Task<MyEnrollmentsDto> SwapAsync(SwapRequest request, CancellationToken ct)
    {
        var studentId = me.UserId;
        await uow.ExecuteInTransactionAsync(async () =>
        {
            var term = await enrollments.GetCurrentTermAsync(ct)
                       ?? throw new BusinessRuleException("There is no current term.", EnrollmentPolicy.WindowClosed);
            var to = await enrollments.LockSectionAsync(request.ToSectionId, ct) ?? throw NotFoundException.For<Section>(request.ToSectionId);
            if (to.TermId != term.Id)
                throw new BusinessRuleException($"{to.Candidate.Label} isn't offered in the current term.", "not_current_term");

            var state = await StateAsync(studentId, term, ct);
            SwapPolicy.Check(state, request.FromSectionId, to.Candidate);

            var old = await enrollments.FindActiveAsync(studentId, request.FromSectionId, ct)
                      ?? throw new BusinessRuleException("You can only swap a section you are enrolled in.", SwapPolicy.NotEnrolled);
            old.Drop(Now);
            enrollments.Add(new Enrollment { StudentId = studentId, SectionId = request.ToSectionId, EnrolledAt = Now });
            enrollments.Add(Action(studentId, EnrollmentActionType.Swap, request.FromSectionId, request.ToSectionId));
            await cart.RemoveIfPresentAsync(studentId, request.ToSectionId, ct);
        }, ct);

        return await GetMineAsync(ct);
    }

    public async Task DropAsync(DropRequest request, CancellationToken ct)
    {
        var studentId = me.UserId;
        await uow.ExecuteInTransactionAsync(async () =>
        {
            var term = await enrollments.GetCurrentTermAsync(ct)
                       ?? throw new BusinessRuleException("There is no current term.", EnrollmentPolicy.DropWindowClosed);
            var enrollment = await enrollments.FindActiveAsync(studentId, request.SectionId, ct)
                             ?? throw new NotFoundException("You aren't enrolled in this section.");
            EnrollmentPolicy.CheckDrop(WindowOf(term));

            // The seat returns to the section on its own: seats are counted from Enrolled rows (rule E8).
            enrollment.Drop(Now);
            enrollments.Add(Action(studentId, EnrollmentActionType.Drop, request.SectionId, to: null));
        }, ct);
    }

    public async Task DropForAbsenceAsync(int enrollmentId, int performedBy, CancellationToken ct)
    {
        var enrollment = await enrollments.FindAsync(enrollmentId, ct) ?? throw NotFoundException.For<Enrollment>(enrollmentId);
        enrollment.DropForAbsence(Now);
        enrollments.Add(new EnrollmentAction
        {
            StudentId = enrollment.StudentId, Action = EnrollmentActionType.AutoDrop, FromSectionId = enrollment.SectionId,
            PerformedBy = performedBy, PerformedAt = Now,
        });
    }

    public async Task CompleteAsync(int enrollmentId, decimal finalPct, string letterGrade, decimal gradePoints, CancellationToken ct)
    {
        var enrollment = await enrollments.FindAsync(enrollmentId, ct) ?? throw NotFoundException.For<Enrollment>(enrollmentId);
        enrollment.Complete(finalPct, letterGrade, gradePoints);

        // Rule G5: only the latest attempt of a course counts in the GPA.
        var courseId = await enrollments.GetCourseIdAsync(enrollment.SectionId, ct);
        foreach (var older in await enrollments.FindCountingAttemptsAsync(enrollment.StudentId, courseId, enrollment.Id, ct))
            older.Supersede();
    }

    private async Task<StudentEnrollmentState> StateAsync(int studentId, CurrentTerm term, CancellationToken ct) => new(
        WindowOf(term),
        await enrollments.GetMaxCreditsAsync(studentId, ct),
        await enrollments.GetHeldSectionsAsync(studentId, term.Id, ct),
        await enrollments.GetPassedCourseIdsAsync(studentId, ct));

    private TermWindow WindowOf(CurrentTerm t) =>
        new Term { EnrollStart = t.EnrollStart, EnrollEnd = t.EnrollEnd, AddDropStart = t.AddDropStart, AddDropEnd = t.AddDropEnd }.WindowAt(Now);

    private EnrollmentAction Action(int studentId, EnrollmentActionType type, int? from, int? to) => new()
    {
        StudentId = studentId, Action = type, FromSectionId = from, ToSectionId = to, PerformedBy = studentId, PerformedAt = Now,
    };
}
