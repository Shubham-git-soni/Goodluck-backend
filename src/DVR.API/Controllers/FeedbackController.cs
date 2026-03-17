using Dapper;
using DVR.Application.Common;
using DVR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVR.API.Controllers;

[ApiController]
[Route("api/feedback")]
[Authorize]
public class FeedbackController : ControllerBase
{
    private readonly IDbConnectionFactory _db;
    private readonly ICurrentUserService _currentUser;

    public FeedbackController(IDbConnectionFactory db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetFeedback([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
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
            where.Add("f.SalesmanId = @SalesmanId"); p.Add("SalesmanId", sid);
        }
        else if (salesmanId.HasValue) { where.Add("f.SalesmanId = @SalesmanId"); p.Add("SalesmanId", salesmanId.Value); }
        if (!string.IsNullOrWhiteSpace(status)) { where.Add("f.Status = @Status"); p.Add("Status", status); }

        var wc = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : string.Empty;
        var total = await conn.QueryFirstOrDefaultAsync<int>($"SELECT COUNT(*) FROM Feedback f {wc}", p);
        var data = await conn.QueryAsync($@"
            SELECT f.*, u.FullName AS SalesmanName,
                s.SchoolName, b.ShopName AS BookSellerName
            FROM Feedback f
            JOIN Salesmen sm ON f.SalesmanId = sm.SalesmanId
            JOIN Users u ON sm.UserId = u.UserId
            LEFT JOIN Schools s ON f.SchoolId = s.SchoolId
            LEFT JOIN BookSellers b ON f.BookSellerId = b.BookSellerId
            {wc} ORDER BY f.CreatedAt DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY", p);

        return Ok(new PaginatedResponse<object>
        {
            Success = true, Message = "Feedback retrieved.", Data = data,
            Pagination = new PaginationMeta { Page = page, PageSize = pageSize, TotalCount = total }
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetFeedbackById(int id)
    {
        using var conn = _db.CreateConnection();
        var feedback = await conn.QueryFirstOrDefaultAsync(@"
            SELECT f.*, u.FullName AS SalesmanName, s.SchoolName, b.ShopName AS BookSellerName
            FROM Feedback f
            JOIN Salesmen sm ON f.SalesmanId = sm.SalesmanId
            JOIN Users u ON sm.UserId = u.UserId
            LEFT JOIN Schools s ON f.SchoolId = s.SchoolId
            LEFT JOIN BookSellers b ON f.BookSellerId = b.BookSellerId
            WHERE f.FeedbackId = @id", new { id });

        return feedback is not null ? Ok(ApiResponse<object>.Ok(feedback)) : NotFound(ApiResponse<object>.Fail("Feedback not found."));
    }

    [HttpPut("{id:int}/resolve")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> ResolveFeedback(int id, [FromBody] ResolveFeedbackRequest request)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(@"
            UPDATE Feedback SET Status = 'Resolved', ResolvedById = @ResolvedById, ResolvedAt = GETUTCDATE(),
                ResolutionNotes = @ResolutionNotes, UpdatedAt = GETUTCDATE()
            WHERE FeedbackId = @id", new { ResolvedById = _currentUser.UserId, request.ResolutionNotes, id });

        return rows > 0 ? Ok(ApiResponse.Ok("Feedback resolved.")) : NotFound(ApiResponse.Fail("Feedback not found."));
    }

    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateFeedbackStatusRequest request)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(@"
            UPDATE Feedback SET Status = @Status, UpdatedAt = GETUTCDATE()
            WHERE FeedbackId = @id", new { request.Status, id });

        return rows > 0 ? Ok(ApiResponse.Ok("Feedback status updated.")) : NotFound(ApiResponse.Fail("Feedback not found."));
    }
}

public class ResolveFeedbackRequest
{
    public string? ResolutionNotes { get; set; }
}

public class UpdateFeedbackStatusRequest
{
    public string Status { get; set; } = string.Empty;
}
