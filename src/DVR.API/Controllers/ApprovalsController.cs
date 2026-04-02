using Dapper;
using DVR.Application.Common;
using DVR.Application.DTOs.Expenses;
using DVR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVR.API.Controllers;

[ApiController]
[Route("api/approvals")]
[Authorize]
public class ApprovalsController : ControllerBase
{
    private readonly IDbConnectionFactory _db;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notifications;

    public ApprovalsController(IDbConnectionFactory db, ICurrentUserService currentUser, INotificationService notifications)
    {
        _db = db;
        _currentUser = currentUser;
        _notifications = notifications;
    }

    // ─── Salesman: submit a new school or bookseller request for approval ─────

    [HttpPost("request")]
    [Authorize(Roles = "Salesman,Admin,Manager")]
    public async Task<IActionResult> SubmitRequest([FromBody] MasterRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.EntityName) || string.IsNullOrWhiteSpace(request.Type))
            return BadRequest(ApiResponse.Fail("EntityName and Type are required."));

        if (request.Type != "School" && request.Type != "BookSeller")
            return BadRequest(ApiResponse.Fail("Type must be 'School' or 'BookSeller'."));

        using var conn = _db.CreateConnection();

        var salesmanId = await conn.QueryFirstOrDefaultAsync<int?>(
            "SELECT SalesmanId FROM Salesmen WHERE UserId = @UserId", new { _currentUser.UserId });

        if (salesmanId == null)
            return BadRequest(ApiResponse.Fail("Salesman profile not found for this user."));

        var requestId = await conn.QueryFirstOrDefaultAsync<int>(@"
            INSERT INTO MasterRequests
                (Type, EntityName, City, State, Address, Phone, Board, Strength, OwnerName, GstNumber, SalesmanId, Status, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.RequestId
            VALUES
                (@Type, @EntityName, @City, @State, @Address, @Phone, @Board, @Strength, @OwnerName, @GstNumber, @SalesmanId, 'Pending', GETUTCDATE(), GETUTCDATE())",
            new
            {
                request.Type,
                request.EntityName,
                request.City,
                request.State,
                request.Address,
                request.Phone,
                request.Board,
                request.Strength,
                request.OwnerName,
                request.GstNumber,
                SalesmanId = salesmanId,
            });

        // Notify Admin and Manager roles in real-time via SignalR (non-blocking — DB insert already succeeded)
        try
        {
            var typeLabel = request.Type == "School" ? "School" : "Book Seller";
            var location  = string.IsNullOrWhiteSpace(request.City) ? "" : $" ({request.City}{(string.IsNullOrWhiteSpace(request.State) ? "" : ", " + request.State)})";
            await _notifications.SendToRoleAsync("Admin",   $"New {typeLabel} Approval Request", $"\"{request.EntityName}\"{location} submitted by a salesman is pending your review.", "master_approval", "/admin/approvals");
            await _notifications.SendToRoleAsync("Manager", $"New {typeLabel} Approval Request", $"\"{request.EntityName}\"{location} submitted by a salesman is pending your review.", "master_approval", "/admin/approvals");
        }
        catch (Exception ex)
        {
            // Log but don't fail the request — the MasterRequest row was already inserted
            Console.WriteLine($"[ApprovalsController] Notification send failed (non-fatal): {ex.Message}");
        }

        return Ok(ApiResponse<object>.Ok(new { requestId }, $"Your request for '{request.EntityName}' has been submitted for approval."));
    }

    // ─── Admin/Manager: get master requests (school/bookseller) ─────────────────

    [HttpGet("master-requests")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetMasterRequests([FromQuery] string? status = null)
    {
        using var conn = _db.CreateConnection();
        var salesmanFilter = _currentUser.IsManager
            ? "AND sm.ManagerId = @ManagerId"
            : string.Empty;
        var statusFilter = !string.IsNullOrWhiteSpace(status) && status != "all"
            ? "AND mr.Status = @Status"
            : string.Empty;

        var p = new DynamicParameters();
        if (_currentUser.IsManager) p.Add("ManagerId", _currentUser.UserId);
        if (!string.IsNullOrWhiteSpace(status) && status != "all") p.Add("Status", status);

        var requests = await conn.QueryAsync($@"
            SELECT
                mr.RequestId        AS id,
                mr.Type             AS type,
                mr.EntityName       AS entityName,
                mr.City             AS city,
                mr.State            AS state,
                mr.Address          AS address,
                mr.Phone            AS phone,
                mr.Board            AS board,
                mr.Strength         AS strength,
                mr.OwnerName        AS ownerName,
                mr.GstNumber        AS gstNumber,
                u.FullName          AS submittedBy,
                sm.SalesmanId       AS salesmanId,
                LOWER(mr.Status)    AS status,
                mr.ReviewerNote     AS reviewerNote,
                mr.ReviewedAt       AS reviewedOn,
                mr.CreatedAt        AS submittedOn
            FROM MasterRequests mr
            JOIN Salesmen sm ON mr.SalesmanId = sm.SalesmanId
            JOIN Users u ON sm.UserId = u.UserId
            WHERE 1=1 {salesmanFilter} {statusFilter}
            ORDER BY mr.CreatedAt DESC", p);

        return Ok(ApiResponse<object>.Ok(requests, "Master requests retrieved."));
    }

    // ─── Admin/Manager: get all pending approvals (TourPlans + MasterRequests) ─

    [HttpGet("pending")]
    [Authorize(Roles = "Admin,Manager")]
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

        var masterRequests = await conn.QueryAsync($@"
            SELECT mr.RequestId AS Id, mr.Type, u.FullName AS SalesmanName,
                mr.EntityName, mr.City, mr.State, mr.Address, mr.Phone,
                mr.Board, mr.Strength, mr.OwnerName, mr.GstNumber,
                mr.Status, mr.ReviewerNote, mr.CreatedAt
            FROM MasterRequests mr
            JOIN Salesmen sm ON mr.SalesmanId = sm.SalesmanId
            JOIN Users u ON sm.UserId = u.UserId
            WHERE mr.Status = 'Pending' {salesmanFilter}", p);

        var pending = tourPlans
            .Concat(expenseReports)
            .Concat(tadaClaims)
            .Concat(masterRequests)
            .OrderByDescending(x => x.CreatedAt);

        return Ok(ApiResponse<object>.Ok(pending, "Pending approvals retrieved."));
    }

    // ─── Admin/Manager: approve or reject a MasterRequest ────────────────────

    [HttpPut("request/{id:int}/approve")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> ApproveMasterRequest(int id, [FromBody] ApproveRejectRequest? body = null)
    {
        using var conn = _db.CreateConnection();

        var req = await conn.QueryFirstOrDefaultAsync<MasterRequestRow>(
            "SELECT * FROM MasterRequests WHERE RequestId = @id AND Status = 'Pending'", new { id });

        if (req == null)
            return NotFound(ApiResponse.Fail("Request not found or already reviewed."));

        // Create the actual school or bookseller record, assigned back to the requesting salesman
        if (req.Type == "School")
        {
            await conn.ExecuteAsync(@"
                INSERT INTO Schools (SchoolName, City, State, Address, Phone, Board, TotalStudents, AssignedSalesmanId, IsActive, CreatedAt, UpdatedAt)
                VALUES (@SchoolName, @City, @State, @Address, @Phone, @Board, @Strength, @AssignedSalesmanId, 1, GETUTCDATE(), GETUTCDATE())",
                new { SchoolName = req.EntityName, req.City, req.State, req.Address, req.Phone, req.Board, req.Strength, AssignedSalesmanId = req.SalesmanId });
        }
        else if (req.Type == "BookSeller")
        {
            await conn.ExecuteAsync(@"
                INSERT INTO BookSellers (ShopName, OwnerName, City, State, Address, Phone, AssignedSalesmanId, IsActive, CreatedAt, UpdatedAt)
                VALUES (@ShopName, @OwnerName, @City, @State, @Address, @Phone, @AssignedSalesmanId, 1, GETUTCDATE(), GETUTCDATE())",
                new { ShopName = req.EntityName, OwnerName = req.OwnerName ?? req.EntityName, req.City, req.State, req.Address, req.Phone, AssignedSalesmanId = req.SalesmanId });
        }

        // Mark request as approved
        await conn.ExecuteAsync(@"
            UPDATE MasterRequests SET Status = 'Approved', ReviewedById = @ReviewerId,
                ReviewedAt = GETUTCDATE(), ReviewerNote = @Note, UpdatedAt = GETUTCDATE()
            WHERE RequestId = @id",
            new { ReviewerId = _currentUser.UserId, Note = body?.Reason, id });

        // Notify the salesman whose request was approved (non-blocking)
        try
        {
            var salesmanUserId = await conn.QueryFirstOrDefaultAsync<int?>(
                "SELECT UserId FROM Salesmen WHERE SalesmanId = @SalesmanId", new { req.SalesmanId });
            if (salesmanUserId.HasValue)
            {
                var typeLabel = req.Type == "School" ? "School" : "Book Seller";
                var noteText  = string.IsNullOrWhiteSpace(body?.Reason) ? "" : $" Note: {body?.Reason}";
                await _notifications.SendToUserAsync(salesmanUserId.Value,
                    $"{typeLabel} Request Approved ✓",
                    $"Your {req.Type.ToLower()} request for \"{req.EntityName}\" has been approved.{noteText}",
                    "master", actionUrl: req.Type == "School" ? "/admin/lists/schools" : "/admin/lists/booksellers");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApprovalsController] Approval notification failed (non-fatal): {ex.Message}");
        }

        return Ok(ApiResponse.Ok($"Request #{id} approved and {req.Type} '{req.EntityName}' created."));
    }

    [HttpPut("request/{id:int}/reject")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> RejectMasterRequest(int id, [FromBody] ApproveRejectRequest body)
    {
        using var conn = _db.CreateConnection();

        var req = await conn.QueryFirstOrDefaultAsync<MasterRequestRow>(
            "SELECT * FROM MasterRequests WHERE RequestId = @id AND Status = 'Pending'", new { id });
        if (req == null)
            return NotFound(ApiResponse.Fail("Request not found or already reviewed."));

        await conn.ExecuteAsync(@"
            UPDATE MasterRequests SET Status = 'Rejected', ReviewedById = @ReviewerId,
                ReviewedAt = GETUTCDATE(), ReviewerNote = @Note, UpdatedAt = GETUTCDATE()
            WHERE RequestId = @id",
            new { ReviewerId = _currentUser.UserId, Note = body.Reason, id });

        // Notify the salesman whose request was rejected (non-blocking)
        try
        {
            var salesmanUserId = await conn.QueryFirstOrDefaultAsync<int?>(
                "SELECT UserId FROM Salesmen WHERE SalesmanId = @SalesmanId", new { req.SalesmanId });
            if (salesmanUserId.HasValue)
            {
                var typeLabel = req.Type == "School" ? "School" : "Book Seller";
                await _notifications.SendToUserAsync(salesmanUserId.Value,
                    $"{typeLabel} Request Rejected",
                    $"Your {req.Type.ToLower()} request for \"{req.EntityName}\" was rejected. Reason: {body.Reason}",
                    "master", actionUrl: "/admin/approvals");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApprovalsController] Rejection notification failed (non-fatal): {ex.Message}");
        }

        return Ok(ApiResponse.Ok($"Request #{id} rejected."));
    }

    // ─── Existing TourPlan / ExpenseReport / TadaClaim approve/reject ─────────

    [HttpPut("{id:int}/approve")]
    [Authorize(Roles = "Admin,Manager")]
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
    [Authorize(Roles = "Admin,Manager")]
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

// ─── DTOs ─────────────────────────────────────────────────────────────────────

public class MasterRequestDto
{
    public string Type        { get; set; } = "";   // "School" | "BookSeller"
    public string EntityName  { get; set; } = "";
    public string? City       { get; set; }
    public string? State      { get; set; }
    public string? Address    { get; set; }
    public string? Phone      { get; set; }
    public string? Board      { get; set; }
    public int?    Strength   { get; set; }
    public string? OwnerName  { get; set; }
    public string? GstNumber  { get; set; }
}

internal class MasterRequestRow
{
    public int     RequestId  { get; set; }
    public int     SalesmanId { get; set; }
    public string  Type       { get; set; } = "";
    public string  EntityName { get; set; } = "";
    public string? City       { get; set; }
    public string? State      { get; set; }
    public string? Address    { get; set; }
    public string? Phone      { get; set; }
    public string? Board      { get; set; }
    public int?    Strength   { get; set; }
    public string? OwnerName  { get; set; }
    public string? GstNumber  { get; set; }
}
