using Dapper;
using DVR.Application.Common;
using DVR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVR.API.Controllers;

[ApiController]
[Route("api/pm-schedules")]
[Authorize]
public class PmSchedulesController : ControllerBase
{
    private readonly IDbConnectionFactory _db;
    private readonly ICurrentUserService _currentUser;

    public PmSchedulesController(IDbConnectionFactory db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetSchedules([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
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
            where.Add("ps.SalesmanId = @SalesmanId"); p.Add("SalesmanId", sid);
        }
        else if (salesmanId.HasValue) { where.Add("ps.SalesmanId = @SalesmanId"); p.Add("SalesmanId", salesmanId.Value); }
        if (!string.IsNullOrWhiteSpace(status)) { where.Add("ps.Status = @Status"); p.Add("Status", status); }

        var wc = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : string.Empty;
        var total = await conn.QueryFirstOrDefaultAsync<int>($"SELECT COUNT(*) FROM PmSchedules ps {wc}", p);
        var data = await conn.QueryAsync($@"
            SELECT ps.*, u.FullName AS SalesmanName, s.SchoolName
            FROM PmSchedules ps
            JOIN Salesmen sm ON ps.SalesmanId = sm.SalesmanId
            JOIN Users u ON sm.UserId = u.UserId
            LEFT JOIN Schools s ON ps.SchoolId = s.SchoolId
            {wc} ORDER BY ps.ScheduleDate DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY", p);

        return Ok(new PaginatedResponse<object>
        {
            Success = true, Message = "PM Schedules retrieved.", Data = data,
            Pagination = new PaginationMeta { Page = page, PageSize = pageSize, TotalCount = total }
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetSchedule(int id)
    {
        using var conn = _db.CreateConnection();
        var schedule = await conn.QueryFirstOrDefaultAsync(@"
            SELECT ps.*, u.FullName AS SalesmanName, s.SchoolName
            FROM PmSchedules ps
            JOIN Salesmen sm ON ps.SalesmanId = sm.SalesmanId
            JOIN Users u ON sm.UserId = u.UserId
            LEFT JOIN Schools s ON ps.SchoolId = s.SchoolId
            WHERE ps.PmScheduleId = @id", new { id });

        return schedule is not null ? Ok(ApiResponse<object>.Ok(schedule)) : NotFound(ApiResponse<object>.Fail("Schedule not found."));
    }

    [HttpGet("calendar")]
    public async Task<IActionResult> GetCalendar([FromQuery] int? month, [FromQuery] int? year)
    {
        using var conn = _db.CreateConnection();
        var m = month ?? DateTime.UtcNow.Month;
        var y = year ?? DateTime.UtcNow.Year;

        var sid = _currentUser.IsSalesman
            ? await conn.QueryFirstOrDefaultAsync<int?>("SELECT SalesmanId FROM Salesmen WHERE UserId = @UserId", new { _currentUser.UserId })
            : (int?)null;

        var where = new List<string> { "MONTH(ps.ScheduleDate) = @m", "YEAR(ps.ScheduleDate) = @y" };
        var p = new DynamicParameters();
        p.Add("m", m); p.Add("y", y);

        if (sid.HasValue) { where.Add("ps.SalesmanId = @SalesmanId"); p.Add("SalesmanId", sid.Value); }

        var data = await conn.QueryAsync($@"
            SELECT ps.PmScheduleId, ps.ScheduleDate, ps.PurposeType, ps.Status, s.SchoolName
            FROM PmSchedules ps
            LEFT JOIN Schools s ON ps.SchoolId = s.SchoolId
            WHERE {string.Join(" AND ", where)}
            ORDER BY ps.ScheduleDate", p);

        return Ok(ApiResponse<object>.Ok(data));
    }

    [HttpPost]
    public async Task<IActionResult> CreateSchedule([FromBody] CreatePmScheduleRequest request)
    {
        using var conn = _db.CreateConnection();
        var salesmanId = await conn.QueryFirstOrDefaultAsync<int?>("SELECT SalesmanId FROM Salesmen WHERE UserId = @UserId", new { _currentUser.UserId });
        if (!salesmanId.HasValue && !_currentUser.IsAdmin)
            return BadRequest(ApiResponse.Fail("Salesman profile not found."));

        var sid = salesmanId ?? request.SalesmanId;
        var id = await conn.QueryFirstOrDefaultAsync<int>(@"
            INSERT INTO PmSchedules (SalesmanId, SchoolId, ScheduleDate, PurposeType, Notes, Status, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.PmScheduleId
            VALUES (@SalesmanId, @SchoolId, @ScheduleDate, @PurposeType, @Notes, 'Scheduled', GETUTCDATE(), GETUTCDATE())",
            new { SalesmanId = sid, request.SchoolId, request.ScheduleDate, request.PurposeType, request.Notes });

        return Created($"/api/pm-schedules/{id}", ApiResponse<object>.Ok(new { PmScheduleId = id }, "Schedule created."));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateSchedule(int id, [FromBody] CreatePmScheduleRequest request)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(@"
            UPDATE PmSchedules SET SchoolId = COALESCE(@SchoolId, SchoolId),
                ScheduleDate = COALESCE(@ScheduleDate, ScheduleDate),
                PurposeType = COALESCE(@PurposeType, PurposeType),
                Notes = COALESCE(@Notes, Notes),
                Status = COALESCE(@Status, Status),
                UpdatedAt = GETUTCDATE()
            WHERE PmScheduleId = @id",
            new { request.SchoolId, request.ScheduleDate, request.PurposeType, request.Notes, request.Status, id });

        return rows > 0 ? Ok(ApiResponse.Ok("Schedule updated.")) : NotFound(ApiResponse.Fail("Schedule not found."));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteSchedule(int id)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync("DELETE FROM PmSchedules WHERE PmScheduleId = @id", new { id });
        return rows > 0 ? Ok(ApiResponse.Ok("Schedule deleted.")) : NotFound(ApiResponse.Fail("Schedule not found."));
    }
}

public class CreatePmScheduleRequest
{
    public int? SalesmanId { get; set; }
    public int? SchoolId { get; set; }
    public DateTime ScheduleDate { get; set; }
    public string? PurposeType { get; set; }
    public string? Notes { get; set; }
    public string? Status { get; set; }
}
