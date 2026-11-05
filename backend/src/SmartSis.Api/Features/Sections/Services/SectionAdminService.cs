using FluentValidation;
using FluentValidation.Results;
using SmartSis.Api.Features.Sections.Interfaces;
using SmartSis.Api.Features.Sections.Models.Dtos;
using SmartSis.Api.Features.Sections.Models.Entities;
using SmartSis.Api.Features.Sections.Rules;
using SmartSis.Api.Shared.Data;
using SmartSis.Api.Shared.Exceptions;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Sections.Services;

/// <summary>
/// Check order on save: referenced IDs exist (400) → code unique (409) → enrollments allow the change (409)
/// → no clash (409) → removed meetings have no attendance (409).
/// </summary>
public sealed class SectionAdminService(ISectionRepository sections, IUnitOfWork uow) : ISectionAdminService
{
    private static readonly string[] Days = ["", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];

    public Task<PagedResult<SectionDto>> SearchAsync(SectionSearchQuery query, CancellationToken ct) =>
        sections.SearchAsync(query.TermId, query.CourseId, query.Search, new PageQuery(query.Page, query.PageSize), ct);

    public async Task<SectionDto> GetAsync(int id, CancellationToken ct) =>
        await sections.GetDetailAsync(id, ct) ?? throw NotFoundException.For<Section>(id);

    public async Task<SectionDto> CreateAsync(SaveSectionRequest request, CancellationToken ct)
    {
        var code = NormalizeCode(request.SectionCode);
        await EnsureReferencesExistAsync(request, ct);
        await EnsureCodeFreeAsync(request, code, excludeId: null, ct);
        await EnsureNoClashAsync(request, excludeId: null, ct);

        var section = new Section
        {
            CourseId = request.CourseId,
            TermId = request.TermId,
            SectionCode = code,
            InstructorId = request.InstructorId,
            Capacity = request.Capacity,
            // Meeting ids are ignored on create: every meeting is new.
            Meetings = [.. request.Meetings.Select(m => Apply(new SectionMeeting(), m))],
        };
        sections.Add(section);
        await uow.SaveChangesAsync(ct);

        return await GetAsync(section.Id, ct);
    }

    public async Task<SectionDto> UpdateAsync(int id, SaveSectionRequest request, CancellationToken ct)
    {
        var section = await sections.GetForUpdateAsync(id, ct) ?? throw NotFoundException.For<Section>(id);
        var code = NormalizeCode(request.SectionCode);
        await EnsureReferencesExistAsync(request, ct);
        EnsureMeetingsBelongTo(section, request);
        await EnsureCodeFreeAsync(request, code, excludeId: id, ct);

        var taken = await sections.CountSeatsTakenAsync(id, ct);
        if (taken > 0 && (section.CourseId != request.CourseId || section.TermId != request.TermId))
            throw new BusinessRuleException(
                "Students are enrolled in this section, so its course and term can't change.", "section_has_enrollments");
        if (request.Capacity < taken)
            throw new BusinessRuleException(
                $"{taken} students already hold a seat, so capacity can't be lower than {taken}.", "capacity_below_taken");

        await EnsureNoClashAsync(request, excludeId: id, ct);

        var keep = request.Meetings.Where(m => m.Id is not null).Select(m => m.Id!.Value).ToHashSet();
        var removed = section.Meetings.Where(m => !keep.Contains(m.Id)).ToList();
        var withAttendance = await sections.MeetingIdsWithAttendanceAsync(removed.Select(m => m.Id), ct);
        var locked = removed.FirstOrDefault(m => withAttendance.Contains(m.Id));
        if (locked is not null)
            throw new BusinessRuleException(
                $"Attendance was already taken for the {Days[locked.DayOfWeek]} {locked.StartTime:HH\\:mm} meeting, so it can't be removed.",
                "meeting_has_attendance");

        section.CourseId = request.CourseId;
        section.TermId = request.TermId;
        section.SectionCode = code;
        section.InstructorId = request.InstructorId;
        section.Capacity = request.Capacity;

        foreach (var meeting in removed)
        {
            sections.RemoveMeeting(meeting);
            section.Meetings.Remove(meeting);
        }
        var existing = section.Meetings.ToDictionary(m => m.Id);
        foreach (var m in request.Meetings)
        {
            if (m.Id is not null) Apply(existing[m.Id.Value], m);
            else section.Meetings.Add(Apply(new SectionMeeting(), m));
        }

        // One SaveChanges = one database transaction for the section and its meetings.
        await uow.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    private static SectionMeeting Apply(SectionMeeting meeting, SaveMeetingRequest r)
    {
        meeting.DayOfWeek = r.DayOfWeek;
        meeting.StartTime = r.StartTime;
        meeting.EndTime = r.EndTime;
        meeting.RoomId = r.RoomId;
        meeting.Type = r.Type;
        return meeting;
    }

    private async Task EnsureNoClashAsync(SaveSectionRequest r, int? excludeId, CancellationToken ct)
    {
        var proposed = r.Meetings.Select(m => new MeetingSlot(m.DayOfWeek, m.StartTime, m.EndTime, m.RoomId)).ToList();
        var others = await sections.GetTermMeetingsAsync(r.TermId, excludeId, ct);

        var clash = SectionClashCheck.Find(proposed, r.InstructorId, others);
        if (clash is null) return;

        var mine = proposed[clash.MeetingIndex];
        throw clash.Kind switch
        {
            ClashKind.SameSection => new BusinessRuleException(
                $"Meetings {clash.MeetingIndex + 1} and {clash.OtherIndex + 1} of this section overlap on {Days[mine.DayOfWeek]}.",
                "meeting_overlap"),
            ClashKind.Room => new BusinessRuleException(
                $"Room {clash.Other!.RoomName} is already used by {clash.Other.SectionLabel} on {Slot(clash.Other)}.",
                "room_clash"),
            _ => new BusinessRuleException(
                $"The instructor already teaches {clash.Other!.SectionLabel} on {Slot(clash.Other)}.",
                "instructor_clash"),
        };
    }

    private static string Slot(ScheduledMeeting m) => $"{Days[m.DayOfWeek]} {m.StartTime:HH\\:mm}–{m.EndTime:HH\\:mm}";

    private async Task EnsureReferencesExistAsync(SaveSectionRequest r, CancellationToken ct)
    {
        var failures = new List<ValidationFailure>();

        if (!await sections.CourseExistsAsync(r.CourseId, ct))
            failures.Add(new("courseId", $"Course {r.CourseId} doesn't exist."));
        if (!await sections.TermExistsAsync(r.TermId, ct))
            failures.Add(new("termId", $"Term {r.TermId} doesn't exist."));
        if (r.InstructorId is { } instructorId && !await sections.ActiveInstructorExistsAsync(instructorId, ct))
            failures.Add(new("instructorId", "Choose an active instructor."));

        var rooms = await sections.ExistingRoomIdsAsync(r.Meetings.Select(m => m.RoomId), ct);
        for (var i = 0; i < r.Meetings.Length; i++)
            if (!rooms.Contains(r.Meetings[i].RoomId))
                failures.Add(new($"meetings[{i}].roomId", $"Room {r.Meetings[i].RoomId} doesn't exist."));

        if (failures.Count > 0) throw new ValidationException(failures);
    }

    private static void EnsureMeetingsBelongTo(Section section, SaveSectionRequest r)
    {
        var own = section.Meetings.Select(m => m.Id).ToHashSet();
        var failures = r.Meetings
            .Select((m, i) => (m, i))
            .Where(x => x.m.Id is not null && !own.Contains(x.m.Id.Value))
            .Select(x => new ValidationFailure($"meetings[{x.i}].id", $"Meeting {x.m.Id} isn't part of this section."))
            .ToList();
        if (failures.Count > 0) throw new ValidationException(failures);
    }

    private async Task EnsureCodeFreeAsync(SaveSectionRequest r, string code, int? excludeId, CancellationToken ct)
    {
        if (await sections.CodeExistsAsync(r.CourseId, r.TermId, code, excludeId, ct))
            throw new BusinessRuleException($"Section {code} already exists for this course in this term.", "duplicate_section_code");
    }

    private static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();
}
