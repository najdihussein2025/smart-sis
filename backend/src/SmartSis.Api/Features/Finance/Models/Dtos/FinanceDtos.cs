using Microsoft.AspNetCore.Mvc;
using SmartSis.Api.Features.Finance.Enums;

namespace SmartSis.Api.Features.Finance.Models.Dtos;

// ---------- Requests ----------

/// <summary>Charge every active student of a major (rule F1). One Charge per student, created in one transaction.</summary>
/// <param name="MajorId">Major whose active students are charged.</param>
/// <param name="TermId">Term the charge belongs to.</param>
/// <param name="PaymentTypeId">Tuition, Registration, Lab fee… (from <c>/api/admin/finance/options</c>).</param>
/// <param name="Title">Shown to the student, e.g. "Fall tuition". 1–128 characters.</param>
/// <param name="Amount">Amount per student, greater than 0, at most 2 decimals.</param>
/// <param name="DueDate">Day the charge is due (yyyy-mm-dd).</param>
public sealed record CreateChargeBatchRequest(int MajorId, int TermId, int PaymentTypeId, string Title, decimal Amount, DateOnly DueDate);

/// <summary>A one-off charge for a single student (no batch).</summary>
/// <param name="UniversityId">The student's University ID. The student must be active.</param>
/// <param name="TermId">Term the charge belongs to.</param>
/// <param name="PaymentTypeId">Payment type id.</param>
/// <param name="Title">Shown to the student. 1–128 characters.</param>
/// <param name="Amount">Greater than 0, at most 2 decimals.</param>
/// <param name="DueDate">Day the charge is due.</param>
public sealed record CreateChargeRequest(long UniversityId, int TermId, int PaymentTypeId, string Title, decimal Amount, DateOnly DueDate);

/// <summary>Record a full or partial payment (rule F2). It can't exceed the remaining balance (rule F3).</summary>
/// <param name="Amount">Greater than 0, at most 2 decimals, at most the remaining balance.</param>
/// <param name="Note">Optional note, e.g. a receipt number. At most 512 characters.</param>
public sealed record RecordPaymentRequest(decimal Amount, string? Note);

/// <summary>Filters and paging for the admin charge list.</summary>
/// <param name="Search">University ID (exact) or part of the student's name.</param>
/// <param name="Status">Pending, Partial or Paid.</param>
/// <param name="TermId">Only charges of this term.</param>
/// <param name="MajorId">Only students of this major.</param>
/// <param name="BatchId">Only charges created by this batch.</param>
/// <param name="Page">1-based page number (default 1).</param>
/// <param name="PageSize">Rows per page, 1–100 (default 20).</param>
public sealed record ChargeSearchQuery(
    [property: FromQuery(Name = "search")] string? Search = null,
    [property: FromQuery(Name = "status")] ChargeStatus? Status = null,
    [property: FromQuery(Name = "termId")] int? TermId = null,
    [property: FromQuery(Name = "majorId")] int? MajorId = null,
    [property: FromQuery(Name = "batchId")] int? BatchId = null,
    [property: FromQuery(Name = "page")] int Page = 1,
    [property: FromQuery(Name = "pageSize")] int PageSize = 20);

/// <summary>Filters and paging for the admin batch list.</summary>
/// <param name="TermId">Only batches of this term.</param>
/// <param name="MajorId">Only batches of this major.</param>
/// <param name="Page">1-based page number (default 1).</param>
/// <param name="PageSize">Rows per page, 1–100 (default 20).</param>
public sealed record ChargeBatchSearchQuery(
    [property: FromQuery(Name = "termId")] int? TermId = null,
    [property: FromQuery(Name = "majorId")] int? MajorId = null,
    [property: FromQuery(Name = "page")] int Page = 1,
    [property: FromQuery(Name = "pageSize")] int PageSize = 20);

// ---------- Responses ----------

/// <summary>Dropdown lists for the admin payments screen.</summary>
public sealed record FinanceOptionsDto(
    IReadOnlyList<FinanceTermOptionDto> Terms,
    IReadOnlyList<FinanceMajorOptionDto> Majors,
    IReadOnlyList<PaymentTypeOptionDto> PaymentTypes);

/// <summary>A term, newest first.</summary>
public sealed record FinanceTermOptionDto(int Id, string Name, bool IsCurrent);

/// <summary>A major and how many active students a batch would charge.</summary>
public sealed record FinanceMajorOptionDto(int Id, string Name, int ActiveStudents);

/// <summary>Tuition, Registration, Lab fee…</summary>
public sealed record PaymentTypeOptionDto(int Id, string Name);

/// <summary>A payment recorded against a charge.</summary>
public sealed record PaymentDto(int Id, decimal Amount, DateTime PaidAt, string? Note);

/// <summary>A charge batch with how many of its charges are paid.</summary>
public sealed record ChargeBatchDto(
    int Id, string Title, int MajorId, string MajorName, int TermId, string TermName, int PaymentTypeId, string PaymentTypeName,
    decimal Amount, DateOnly DueDate, int ChargeCount, int PaidCount, DateTime CreatedAt);

/// <summary>A charge as the admin sees it. <c>Status</c> follows the payments (rule F2).</summary>
public sealed record AdminChargeDto(
    int Id, int StudentId, long UniversityId, string StudentName, string MajorName, int? BatchId,
    int TermId, string TermName, string PaymentTypeName, string Title,
    decimal Amount, decimal Paid, decimal Balance, DateOnly DueDate, ChargeStatus Status, bool IsOverdue,
    IReadOnlyList<PaymentDto> Payments);

/// <summary>The signed-in student's financial account.</summary>
/// <param name="TotalCharged">Sum of all charges.</param>
/// <param name="TotalPaid">Sum of all payments.</param>
/// <param name="OutstandingBalance">What is still owed (charged − paid).</param>
/// <param name="OverdueBalance">Part of the outstanding balance whose due date has passed.</param>
/// <param name="NextDueDate">Earliest due date today or later among unpaid charges; null when none.</param>
/// <param name="Charges">Every charge, by due date.</param>
public sealed record MyFinanceDto(
    decimal TotalCharged, decimal TotalPaid, decimal OutstandingBalance, decimal OverdueBalance, DateOnly? NextDueDate,
    IReadOnlyList<StudentChargeDto> Charges);

/// <summary>One of the student's charges with its payments.</summary>
public sealed record StudentChargeDto(
    int Id, string Title, int TermId, string TermName, string PaymentTypeName,
    decimal Amount, decimal Paid, decimal Balance, DateOnly DueDate, ChargeStatus Status, bool IsOverdue,
    IReadOnlyList<PaymentDto> Payments);
