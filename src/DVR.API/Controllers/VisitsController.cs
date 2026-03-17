using Dapper;
using DVR.Application.Common;
using DVR.Application.DTOs.Visits;
using DVR.Application.Features.Visits.Commands;
using DVR.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVR.API.Controllers;

[ApiController]
[Route("api/visits")]
[Authorize]
public class VisitsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IDbConnectionFactory _db;
    private readonly ICurrentUserService _currentUser;

    public VisitsController(IMediator mediator, IDbConnectionFactory db, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetVisits([FromQuery] VisitQueryParams query)
    {
        using var conn = _db.CreateConnection();
        var where = new List<string>();
        var p = new DynamicParameters();
        p.Add("Offset", query.Offset);
        p.Add("PageSize", query.PageSize);

        if (_currentUser.IsSalesman)
        {
            var sid = await conn.QueryFirstOrDefaultAsync<int?>("SELECT SalesmanId FROM Salesmen WHERE UserId = @UserId", new { _currentUser.UserId });
            where.Add("v.SalesmanId = @SalesmanId"); p.Add("SalesmanId", sid);
        }
        else if (_currentUser.IsManager)
        {
            where.Add("v.SalesmanId IN (SELECT SalesmanId FROM Salesmen WHERE ManagerId = @ManagerId)");
            p.Add("ManagerId", _currentUser.UserId);
        }
        else if (query.SalesmanId.HasValue) { where.Add("v.SalesmanId = @SalesmanId"); p.Add("SalesmanId", query.SalesmanId.Value); }

        if (!string.IsNullOrWhiteSpace(query.VisitType))
        {
            var vt = query.VisitType == "School" ? 1 : 2;
            where.Add("v.VisitType = @VisitType"); p.Add("VisitType", vt);
        }
        if (query.FromDate.HasValue) { where.Add("v.VisitDate >= @FromDate"); p.Add("FromDate", query.FromDate.Value); }
        if (query.ToDate.HasValue) { where.Add("v.VisitDate <= @ToDate"); p.Add("ToDate", query.ToDate.Value.AddDays(1)); }

        var wc = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : string.Empty;
        var total = await conn.QueryFirstOrDefaultAsync<int>($"SELECT COUNT(*) FROM Visits v {wc}", p);
        var data = await conn.QueryAsync<VisitDto>($@"
            SELECT v.VisitId, v.SalesmanId, u.FullName AS SalesmanName,
                CASE v.VisitType WHEN 1 THEN 'School' ELSE 'Bookseller' END AS VisitType,
                v.SchoolId, s.SchoolName, v.BookSellerId, b.ShopName AS BookSellerName,
                v.VisitDate, v.Purpose, v.Remarks, v.Outcome, v.FollowUpDate,
                v.CheckInLatitude, v.CheckInLongitude, v.CheckOutLatitude, v.CheckOutLongitude,
                v.PhotoUrl, v.IsCompleted, v.CreatedAt
            FROM Visits v
            JOIN Salesmen sm ON v.SalesmanId = sm.SalesmanId
            JOIN Users u ON sm.UserId = u.UserId
            LEFT JOIN Schools s ON v.SchoolId = s.SchoolId
            LEFT JOIN BookSellers b ON v.BookSellerId = b.BookSellerId
            {wc}
            ORDER BY v.VisitDate DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY", p);

        return Ok(new PaginatedResponse<VisitDto>
        {
            Success = true, Message = "Visits retrieved.", Data = data,
            Pagination = new PaginationMeta { Page = query.Page, PageSize = query.PageSize, TotalCount = total }
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetVisit(int id)
    {
        using var conn = _db.CreateConnection();
        var visit = await conn.QueryFirstOrDefaultAsync<VisitDto>(@"
            SELECT v.VisitId, v.SalesmanId, u.FullName AS SalesmanName,
                CASE v.VisitType WHEN 1 THEN 'School' ELSE 'Bookseller' END AS VisitType,
                v.SchoolId, s.SchoolName, v.BookSellerId, b.ShopName AS BookSellerName,
                v.VisitDate, v.Purpose, v.Remarks, v.Outcome, v.FollowUpDate,
                v.CheckInLatitude, v.CheckInLongitude, v.CheckOutLatitude, v.CheckOutLongitude,
                v.PhotoUrl, v.IsCompleted, v.CreatedAt
            FROM Visits v
            JOIN Salesmen sm ON v.SalesmanId = sm.SalesmanId
            JOIN Users u ON sm.UserId = u.UserId
            LEFT JOIN Schools s ON v.SchoolId = s.SchoolId
            LEFT JOIN BookSellers b ON v.BookSellerId = b.BookSellerId
            WHERE v.VisitId = @id", new { id });

        return visit is not null ? Ok(ApiResponse<VisitDto>.Ok(visit)) : NotFound(ApiResponse<VisitDto>.Fail("Visit not found."));
    }

    [HttpPost("school")]
    public async Task<IActionResult> CreateSchoolVisit([FromBody] CreateSchoolVisitRequest request)
    {
        var result = await _mediator.Send(new CreateSchoolVisitCommand { Request = request });
        return result.Success ? Created($"/api/visits/{result.Data?.VisitId}", result) : BadRequest(result);
    }

    [HttpPost("bookseller")]
    public async Task<IActionResult> CreateBooksellerVisit([FromBody] CreateBooksellerVisitRequest request)
    {
        var result = await _mediator.Send(new CreateBooksellerVisitCommand { Request = request });
        return result.Success ? Created($"/api/visits/{result.Data?.VisitId}", result) : BadRequest(result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateVisit(int id, [FromBody] UpdateVisitRequest request)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(@"
            UPDATE Visits SET
                Remarks = COALESCE(@Remarks, Remarks),
                Outcome = COALESCE(@Outcome, Outcome),
                FollowUpDate = COALESCE(@FollowUpDate, FollowUpDate),
                CheckOutLatitude = COALESCE(@CheckOutLatitude, CheckOutLatitude),
                CheckOutLongitude = COALESCE(@CheckOutLongitude, CheckOutLongitude),
                PhotoUrl = COALESCE(@PhotoUrl, PhotoUrl),
                IsCompleted = COALESCE(@IsCompleted, IsCompleted),
                UpdatedAt = GETUTCDATE()
            WHERE VisitId = @id", new { request.Remarks, request.Outcome, request.FollowUpDate, request.CheckOutLatitude, request.CheckOutLongitude, request.PhotoUrl, request.IsCompleted, id });

        return rows > 0 ? Ok(ApiResponse.Ok("Visit updated.")) : NotFound(ApiResponse.Fail("Visit not found."));
    }
}

public class UpdateVisitRequest
{
    public string? Remarks { get; set; }
    public string? Outcome { get; set; }
    public string? FollowUpDate { get; set; }
    public string? CheckOutLatitude { get; set; }
    public string? CheckOutLongitude { get; set; }
    public string? PhotoUrl { get; set; }
    public bool? IsCompleted { get; set; }
}
