using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Chat.Enums;
using SmartSis.Api.Features.Chat.Interfaces;
using SmartSis.Api.Features.Chat.Models.Entities;
using SmartSis.Api.Features.Courses.Models.Entities;
using SmartSis.Api.Features.Enrollments.Enums;
using SmartSis.Api.Features.Enrollments.Models.Entities;
using SmartSis.Api.Shared.Data;

namespace SmartSis.Api.Features.Chat.Repositories;

public sealed class ChatRepository(SisDbContext db) : IChatRepository
{
    public Task<List<ConversationRow>> ListAsync(int studentId, CancellationToken ct) =>
        db.Set<ChatConversation>().AsNoTracking()
            .Where(c => c.StudentId == studentId)
            .OrderByDescending(c => c.UpdatedAt).ThenByDescending(c => c.Id)
            .Select(c => new ConversationRow(
                c.Id, c.Title,
                c.Messages.Where(m => m.Role == ChatRole.Tool).OrderBy(m => m.Id).Select(m => m.Content).FirstOrDefault(),
                c.CreatedAt, c.UpdatedAt))
            .ToListAsync(ct);

    public Task<ChatConversation?> FindOwnAsync(int id, int studentId, CancellationToken ct) =>
        db.Set<ChatConversation>().FirstOrDefaultAsync(c => c.Id == id && c.StudentId == studentId, ct);

    public Task<List<ChatMessage>> MessagesAsync(int conversationId, CancellationToken ct) =>
        db.Set<ChatMessage>().AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAt).ThenBy(m => m.Id)
            .ToListAsync(ct);

    public Task<TutorCourse?> GetEnrolledCourseAsync(int studentId, int courseId, CancellationToken ct) =>
        db.Set<Enrollment>().AsNoTracking()
            .Where(e => e.StudentId == studentId && e.Status == EnrollmentStatus.Enrolled && e.Section.CourseId == courseId)
            .Select(e => new TutorCourse(e.Section.Course.Id, e.Section.Course.Code, e.Section.Course.Name, e.Section.Course.Description))
            .FirstOrDefaultAsync(ct);

    public Task<TutorCourse?> GetCourseAsync(int courseId, CancellationToken ct) =>
        db.Set<Course>().AsNoTracking().Where(c => c.Id == courseId)
            .Select(c => new TutorCourse(c.Id, c.Code, c.Name, c.Description))
            .FirstOrDefaultAsync(ct);

    public void Add(ChatConversation conversation) => db.Set<ChatConversation>().Add(conversation);
    public void Add(ChatMessage message) => db.Set<ChatMessage>().Add(message);

    /// <summary>Soft delete: the SoftDeleteInterceptor turns this into <c>DeletedAt = now</c>.</summary>
    public void Remove(ChatConversation conversation) => db.Set<ChatConversation>().Remove(conversation);
}
