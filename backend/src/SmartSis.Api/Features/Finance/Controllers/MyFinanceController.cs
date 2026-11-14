using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSis.Api.Features.Finance.Interfaces;
using SmartSis.Api.Features.Finance.Models.Dtos;
using SmartSis.Api.Shared.Auth;

namespace SmartSis.Api.Features.Finance.Controllers;

/// <summary>Student: my financial account (PB-18). Only the signed-in student's own charges.</summary>
[ApiController]
[Authorize(Policy = Policies.Student)]
[Route("api/finance")]
[Tags("Student: Finance")]
public sealed class MyFinanceController(IMyFinanceService finance) : ControllerBase
{
    /// <summary>
    /// My charges (by due date) with paid amount, balance, status, overdue flag and payments, plus totals and the outstanding balance.
    /// The AI chatbot calls this with the student's own token to answer questions about deadlines.
    /// </summary>
    [HttpGet("my")]
    public Task<MyFinanceDto> Mine(CancellationToken ct) => finance.GetMineAsync(ct);
}
