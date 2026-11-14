using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSis.Api.Features.Dashboard.Interfaces;
using SmartSis.Api.Features.Dashboard.Models.Dtos;
using SmartSis.Api.Shared.Auth;

namespace SmartSis.Api.Features.Dashboard.Controllers;

/// <summary>Numbers for the role dashboards (PB-22). Each action declares its role; the user comes from the token.</summary>
[ApiController]
[Route("api/dashboard")]
[Tags("Dashboard")]
public sealed class DashboardController(IDashboardService dashboard) : ControllerBase
{
    /// <summary>Student: GPA, credits this term vs cap, classes today, balance, unread notifications, attendance warnings.</summary>
    [HttpGet("student")]
    [Authorize(Policy = Policies.Student)]
    public Task<StudentDashboardDto> Student(CancellationToken ct) => dashboard.GetStudentAsync(ct);

    /// <summary>Instructor: sections this term, classes today, missing scores, attendance still to take this week.</summary>
    [HttpGet("instructor")]
    [Authorize(Policy = Policies.Instructor)]
    public Task<InstructorDashboardDto> Instructor(CancellationToken ct) => dashboard.GetInstructorAsync(ct);

    /// <summary>Admin: active users, sections this term, open support requests, unpaid charges.</summary>
    [HttpGet("admin")]
    [Authorize(Policy = Policies.Admin)]
    public Task<AdminDashboardDto> Admin(CancellationToken ct) => dashboard.GetAdminAsync(ct);
}
