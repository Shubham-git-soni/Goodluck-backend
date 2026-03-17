using Dapper;
using DVR.Application.Common;
using DVR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVR.API.Controllers;

[ApiController]
[Route("api/tour-plans")]
[Authorize]
public class TourPlansController : ControllerBase
{
    private readonly IDbConnectionFactory _db;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notifications;

    public TourPlansController(IDbConnectionFactory db, ICurrentUserService currentUser, INotificationService notifications)
    {
        _db = db;
        _currentUser = currentUser;
        _notifications = notifications;
    }

    [HttpGet]
    public async Task<IActionResult> GetTourPlans([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null, [FromQuery] int? salesmanId = null)
    {
        using var conn = _db.CreateConnection();
        var where = new List<string>();
        var p = new DynamicParameters();
        p.Add("Offset", (page - 1) * pageSize);
        p.Add("PageSize", pageSize);

        if (_currentUser.IsSalesman)
        {
            var sid = await conn.QueryFirstOrDefaultAsync<int?>("SELECT SalesmanId FROM Salesmen WHERE UserId = @UserId", new { _currentUser.UserId });
            where.Add("tp.SalesmanId = @SalesmanId"); p.Add("SalesmanId", sid);
        }
        else if (_currentUser.IsManager)
        {
            where.Add("tp.SalesmanId IN (SELECT SalesmanId FROM Salesmen WHERE ManagerId = @ManagerId)");
            p.Add("ManagerId", _currentUser.UserId);
        }
        else if (salesmanId.HasValue) { where.Add("tp.SalesmanId = @SalesmanId"); p.Add("SalesmanId", salesmanId.Value); }

        if (!string.IsNullOrWhiteSpace(status)) { where.Add("tp.Status = @Status"); p.Add("Status", status); }

        var wc = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : string.Empty;
        var total = await conn.QueryFirstOrDefaultAsync<int>($"SELECT COUNT(*) FROM TourPlans tp {wc}", p);
        var data = await conn.QueryAsync($@"
            SELECT tp.*, u.FullName AS SalesmanName
            FROM TourPlans tp
            JOIN Salesmen sm ON tp.SalesmanId = sm.SalesmanId
            JOIN Users u ON sm.UserId = u.UserId
            {wc} ORDER BY tp.PlanDate DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY", p);

        return Ok(new PaginatedResponse<object>
        {
            Success = true, Message = "Tour plans retrieved.", Data = data,
            Pagination = new PaginationMeta { Page = page, PageSize = pageSize, TotalCount = total }
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetTourPlan(int id)
    {
        using var conn = _db.CreateConnection();
        var tp = await conn.QueryFirstOrDefaultAsync(@"
            SELECT tp.*, u.FullName AS SalesmanName
            FROM TourPlans tp
            JOIN Salesmen sm ON tp.SalesmanId = sm.SalesmanId
            JOIN Users u ON sm.UserId = u.UserId
            WHERE tp.TourPlanId = @id", new { id });

        return tp is not null ? Ok(ApiResponse<object>.Ok(tp)) : NotFound(ApiResponse<object>.Fail("Tour plan not found."));
    }

    [HttpPost]
    public async Task<IActionResult> CreateTourPlan([FromBody] CreateTourPlanRequest request)
    {
        using var conn = _db.CreateConnection();
        var salesmanId = await conn.QueryFirstOrDefaultAsync<int?>("SELECT SalesmanId FROM Salesmen WHERE UserId = @UserId", new { _currentUser.UserId });
        if (!salesmanId.HasValue)
            return BadRequest(ApiResponse.Fail("Salesman profile not found."));

        var id = await conn.QueryFirstOrDefaultAsync<int>(@"
            INSERT INTO TourPlans (SalesmanId, PlanDate, PlannedAreas, PlannedVisits, Status, Remarks, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.TourPlanId
            VALUES (@SalesmanId, @PlanDate, @PlannedAreas, @PlannedVisits, 'Draft', @Remarks, GETUTCDATE(), GETUTCDATE())",
            new { SalesmanId = salesmanId.Value, request.PlanDate, request.PlannedAreas, request.PlannedVisits, request.Remarks });

        return Created($"/api/tour-plans/{id}", ApiResponse<object>.Ok(new { TourPlanId = id }, "Tour plan created."));
    }

    [HttpPut("{id:int}/submit")]
    public async Task<IActionResult> SubmitTourPlan(int id)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(
            "UPDATE TourPlans SET Status = 'Submitted', UpdatedAt = GETUTCDATE() WHERE TourPlanId = @id AND Status = 'Draft'", new { id });

        if (rows > 0)
            await _notifications.SendToRoleAsync("Manager", "Tour Plan Submitted", $"A tour plan #{id} has been submitted for approval.", "TourPlan");

        return rows > 0 ? Ok(ApiResponse.Ok("Tour plan submitted for approval.")) : NotFound(ApiResponse.Fail("Tour plan not found or already submitted."));
    }

    [HttpPut("{id:int}/approve")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> ApproveTourPlan(int id)
    {
        using var conn = _db.CreateConnection();
        var tp = await conn.QueryFirstOrDefaultAsync<DVR.Domain.Entities.TourPlan>("SELECT * FROM TourPlans WHERE TourPlanId = @id", new { id });
        if (tp is null) return NotFound(ApiResponse.Fail("Tour plan not found."));

        await conn.ExecuteAsync(@"
            UPDATE TourPlans SET Status = 'Approved', ApprovedById = @ApprovedById, ApprovedAt = GETUTCDATE(), UpdatedAt = GETUTCDATE()
            WHERE TourPlanId = @id", new { ApprovedById = _currentUser.UserId, id });

        var salesmanUserId = await conn.QueryFirstOrDefaultAsync<int?>("SELECT UserId FROM Salesmen WHERE SalesmanId = @SalesmanId", new { tp.SalesmanId });
        if (salesmanUserId.HasValue)
            await _notifications.SendToUserAsync(salesmanUserId.Value, "Tour Plan Approved", $"Your tour plan #{id} has been approved.");

        return Ok(ApiResponse.Ok("Tour plan approved."));
    }

    [HttpPut("{id:int}/reject")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> RejectTourPlan(int id, [FromBody] DVR.Application.DTOs.Expenses.ApproveRejectRequest request)
    {
        using var conn = _db.CreateConnection();
        var tp = await conn.QueryFirstOrDefaultAsync<DVR.Domain.Entities.TourPlan>("SELECT * FROM TourPlans WHERE TourPlanId = @id", new { id });
        if (tp is null) return NotFound(ApiResponse.Fail("Tour plan not found."));

        await conn.ExecuteAsync(@"
            UPDATE TourPlans SET Status = 'Rejected', ApprovedById = @ApprovedById, ApprovedAt = GETUTCDATE(),
                RejectionReason = @Reason, UpdatedAt = GETUTCDATE()
            WHERE TourPlanId = @id", new { ApprovedById = _currentUser.UserId, request.Reason, id });

        var salesmanUserId = await conn.QueryFirstOrDefaultAsync<int?>("SELECT UserId FROM Salesmen WHERE SalesmanId = @SalesmanId", new { tp.SalesmanId });
        if (salesmanUserId.HasValue)
            await _notifications.SendToUserAsync(salesmanUserId.Value, "Tour Plan Rejected", $"Your tour plan #{id} was rejected. Reason: {request.Reason}");

        return Ok(ApiResponse.Ok("Tour plan rejected."));
    }
}

public class CreateTourPlanRequest
{
    public DateTime PlanDate { get; set; }
    public string? PlannedAreas { get; set; }
    public string? PlannedVisits { get; set; }
    public string? Remarks { get; set; }
}
