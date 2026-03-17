using Dapper;
using DVR.Application.Common;
using DVR.Application.DTOs.Expenses;
using DVR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVR.API.Controllers;

[ApiController]
[Route("api/tada")]
[Authorize]
public class TadaController : ControllerBase
{
    private readonly IDbConnectionFactory _db;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notifications;

    public TadaController(IDbConnectionFactory db, ICurrentUserService currentUser, INotificationService notifications)
    {
        _db = db;
        _currentUser = currentUser;
        _notifications = notifications;
    }

    [HttpGet]
    public async Task<IActionResult> GetTadaClaims([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
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
            where.Add("tc.SalesmanId = @SalesmanId"); p.Add("SalesmanId", sid);
        }
        else if (salesmanId.HasValue) { where.Add("tc.SalesmanId = @SalesmanId"); p.Add("SalesmanId", salesmanId.Value); }
        if (!string.IsNullOrWhiteSpace(status)) { where.Add("tc.Status = @Status"); p.Add("Status", status); }

        var wc = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : string.Empty;
        var total = await conn.QueryFirstOrDefaultAsync<int>($"SELECT COUNT(*) FROM TadaClaims tc {wc}", p);
        var data = await conn.QueryAsync($@"
            SELECT tc.*, u.FullName AS SalesmanName
            FROM TadaClaims tc
            JOIN Salesmen sm ON tc.SalesmanId = sm.SalesmanId
            JOIN Users u ON sm.UserId = u.UserId
            {wc} ORDER BY tc.ClaimYear DESC, tc.ClaimMonth DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY", p);

        return Ok(new PaginatedResponse<object>
        {
            Success = true, Message = "TADA claims retrieved.", Data = data,
            Pagination = new PaginationMeta { Page = page, PageSize = pageSize, TotalCount = total }
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetTadaClaim(int id)
    {
        using var conn = _db.CreateConnection();
        var claim = await conn.QueryFirstOrDefaultAsync(@"
            SELECT tc.*, u.FullName AS SalesmanName
            FROM TadaClaims tc
            JOIN Salesmen sm ON tc.SalesmanId = sm.SalesmanId
            JOIN Users u ON sm.UserId = u.UserId
            WHERE tc.TadaClaimId = @id", new { id });

        return claim is not null ? Ok(ApiResponse<object>.Ok(claim)) : NotFound(ApiResponse<object>.Fail("TADA claim not found."));
    }

    [HttpPost]
    public async Task<IActionResult> CreateTadaClaim([FromBody] CreateTadaClaimRequest request)
    {
        using var conn = _db.CreateConnection();
        var salesmanId = await conn.QueryFirstOrDefaultAsync<int?>("SELECT SalesmanId FROM Salesmen WHERE UserId = @UserId", new { _currentUser.UserId });
        if (!salesmanId.HasValue)
            return BadRequest(ApiResponse.Fail("Salesman profile not found."));

        var total = request.TravelAmount + request.DailyAllowanceAmount;
        var id = await conn.QueryFirstOrDefaultAsync<int>(@"
            INSERT INTO TadaClaims (SalesmanId, ClaimMonth, ClaimYear, TravelAmount, DailyAllowanceAmount, TotalAmount, Status, Remarks, SupportingDocUrl, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.TadaClaimId
            VALUES (@SalesmanId, @ClaimMonth, @ClaimYear, @TravelAmount, @DailyAllowanceAmount, @TotalAmount, 'Draft', @Remarks, @SupportingDocUrl, GETUTCDATE(), GETUTCDATE())",
            new { SalesmanId = salesmanId.Value, request.ClaimMonth, request.ClaimYear, request.TravelAmount, request.DailyAllowanceAmount, TotalAmount = total, request.Remarks, request.SupportingDocUrl });

        return Created($"/api/tada/{id}", ApiResponse<object>.Ok(new { TadaClaimId = id }, "TADA claim created."));
    }

    [HttpPut("{id:int}/submit")]
    public async Task<IActionResult> SubmitClaim(int id)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(
            "UPDATE TadaClaims SET Status = 'Submitted', UpdatedAt = GETUTCDATE() WHERE TadaClaimId = @id AND Status = 'Draft'", new { id });

        if (rows > 0)
            await _notifications.SendToRoleAsync("Manager", "TADA Claim Submitted", $"TADA claim #{id} has been submitted for approval.", "TadaClaim");

        return rows > 0 ? Ok(ApiResponse.Ok("TADA claim submitted.")) : NotFound(ApiResponse.Fail("Claim not found."));
    }

    [HttpPut("{id:int}/approve")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> ApproveClaim(int id)
    {
        using var conn = _db.CreateConnection();
        var claim = await conn.QueryFirstOrDefaultAsync<DVR.Domain.Entities.TadaClaim>("SELECT * FROM TadaClaims WHERE TadaClaimId = @id", new { id });
        if (claim is null) return NotFound(ApiResponse.Fail("Claim not found."));

        await conn.ExecuteAsync(@"
            UPDATE TadaClaims SET Status = 'Approved', ApprovedById = @ApprovedById, ApprovedAt = GETUTCDATE(), UpdatedAt = GETUTCDATE()
            WHERE TadaClaimId = @id", new { ApprovedById = _currentUser.UserId, id });

        var userId = await conn.QueryFirstOrDefaultAsync<int?>("SELECT UserId FROM Salesmen WHERE SalesmanId = @SalesmanId", new { claim.SalesmanId });
        if (userId.HasValue)
            await _notifications.SendToUserAsync(userId.Value, "TADA Claim Approved", $"Your TADA claim #{id} (₹{claim.TotalAmount}) has been approved.");

        return Ok(ApiResponse.Ok("TADA claim approved."));
    }

    [HttpPut("{id:int}/reject")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> RejectClaim(int id, [FromBody] ApproveRejectRequest request)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(@"
            UPDATE TadaClaims SET Status = 'Rejected', ApprovedById = @ApprovedById, ApprovedAt = GETUTCDATE(),
                RejectionReason = @Reason, UpdatedAt = GETUTCDATE()
            WHERE TadaClaimId = @id", new { ApprovedById = _currentUser.UserId, request.Reason, id });

        return Ok(ApiResponse.Ok("TADA claim rejected."));
    }
}

public class CreateTadaClaimRequest
{
    public string ClaimMonth { get; set; } = string.Empty;
    public int ClaimYear { get; set; }
    public decimal TravelAmount { get; set; }
    public decimal DailyAllowanceAmount { get; set; }
    public string? Remarks { get; set; }
    public string? SupportingDocUrl { get; set; }
}
