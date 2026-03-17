using Dapper;
using DVR.Application.Common;
using DVR.Application.DTOs.Expenses;
using DVR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVR.API.Controllers;

[ApiController]
[Route("api/approvals")]
[Authorize(Roles = "Admin,Manager")]
public class ApprovalsController : ControllerBase
{
    private readonly IDbConnectionFactory _db;
    private readonly ICurrentUserService _currentUser;

    public ApprovalsController(IDbConnectionFactory db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingApprovals()
    {
        using var conn = _db.CreateConnection();
        var salesmanFilter = _currentUser.IsManager
            ? "AND sm.ManagerId = @ManagerId"
            : string.Empty;
        var p = new DynamicParameters();
        if (_currentUser.IsManager) p.Add("ManagerId", _currentUser.UserId);

        var tourPlans = await conn.QueryAsync($@"
            SELECT tp.TourPlanId AS Id, 'TourPlan' AS Type, u.FullName AS SalesmanName,
                tp.PlanDate AS ReferenceDate, tp.Status, tp.CreatedAt
            FROM TourPlans tp
            JOIN Salesmen sm ON tp.SalesmanId = sm.SalesmanId
            JOIN Users u ON sm.UserId = u.UserId
            WHERE tp.Status = 'Submitted' {salesmanFilter}", p);

        var expenseReports = await conn.QueryAsync($@"
            SELECT er.ExpenseReportId AS Id, 'ExpenseReport' AS Type, u.FullName AS SalesmanName,
                CAST(er.ReportYear AS NVARCHAR) + '-' + er.ReportMonth AS ReferenceDate, er.Status, er.CreatedAt
            FROM ExpenseReports er
            JOIN Salesmen sm ON er.SalesmanId = sm.SalesmanId
            JOIN Users u ON sm.UserId = u.UserId
            WHERE er.Status = 'Submitted' {salesmanFilter}", p);

        var tadaClaims = await conn.QueryAsync($@"
            SELECT tc.TadaClaimId AS Id, 'TadaClaim' AS Type, u.FullName AS SalesmanName,
                CAST(tc.ClaimYear AS NVARCHAR) + '-' + tc.ClaimMonth AS ReferenceDate, tc.Status, tc.CreatedAt
            FROM TadaClaims tc
            JOIN Salesmen sm ON tc.SalesmanId = sm.SalesmanId
            JOIN Users u ON sm.UserId = u.UserId
            WHERE tc.Status = 'Submitted' {salesmanFilter}", p);

        var pending = tourPlans.Concat(expenseReports).Concat(tadaClaims)
            .OrderByDescending(x => x.CreatedAt);

        return Ok(ApiResponse<object>.Ok(pending, $"Pending approvals retrieved."));
    }

    [HttpPut("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id, [FromQuery] string type, [FromBody] ApproveRejectRequest? request = null)
    {
        using var conn = _db.CreateConnection();
        var rows = type switch
        {
            "TourPlan" => await conn.ExecuteAsync(
                "UPDATE TourPlans SET Status = 'Approved', ApprovedById = @ApproverId, ApprovedAt = GETUTCDATE(), UpdatedAt = GETUTCDATE() WHERE TourPlanId = @id",
                new { ApproverId = _currentUser.UserId, id }),
            "ExpenseReport" => await conn.ExecuteAsync(
                "UPDATE ExpenseReports SET Status = 'Approved', ApprovedById = @ApproverId, ApprovedAt = GETUTCDATE(), UpdatedAt = GETUTCDATE() WHERE ExpenseReportId = @id",
                new { ApproverId = _currentUser.UserId, id }),
            "TadaClaim" => await conn.ExecuteAsync(
                "UPDATE TadaClaims SET Status = 'Approved', ApprovedById = @ApproverId, ApprovedAt = GETUTCDATE(), UpdatedAt = GETUTCDATE() WHERE TadaClaimId = @id",
                new { ApproverId = _currentUser.UserId, id }),
            _ => 0
        };

        return rows > 0 ? Ok(ApiResponse.Ok($"{type} #{id} approved.")) : NotFound(ApiResponse.Fail("Record not found."));
    }

    [HttpPut("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, [FromQuery] string type, [FromBody] ApproveRejectRequest request)
    {
        using var conn = _db.CreateConnection();
        var rows = type switch
        {
            "TourPlan" => await conn.ExecuteAsync(
                "UPDATE TourPlans SET Status = 'Rejected', ApprovedById = @ApproverId, ApprovedAt = GETUTCDATE(), RejectionReason = @Reason, UpdatedAt = GETUTCDATE() WHERE TourPlanId = @id",
                new { ApproverId = _currentUser.UserId, request.Reason, id }),
            "ExpenseReport" => await conn.ExecuteAsync(
                "UPDATE ExpenseReports SET Status = 'Rejected', ApprovedById = @ApproverId, ApprovedAt = GETUTCDATE(), RejectionReason = @Reason, UpdatedAt = GETUTCDATE() WHERE ExpenseReportId = @id",
                new { ApproverId = _currentUser.UserId, request.Reason, id }),
            "TadaClaim" => await conn.ExecuteAsync(
                "UPDATE TadaClaims SET Status = 'Rejected', ApprovedById = @ApproverId, ApprovedAt = GETUTCDATE(), RejectionReason = @Reason, UpdatedAt = GETUTCDATE() WHERE TadaClaimId = @id",
                new { ApproverId = _currentUser.UserId, request.Reason, id }),
            _ => 0
        };

        return rows > 0 ? Ok(ApiResponse.Ok($"{type} #{id} rejected.")) : NotFound(ApiResponse.Fail("Record not found."));
    }
}
