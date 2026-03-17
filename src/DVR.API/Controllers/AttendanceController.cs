using Dapper;
using DVR.Application.Common;
using DVR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVR.API.Controllers;

[ApiController]
[Route("api/attendance")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly IDbConnectionFactory _db;
    private readonly ICurrentUserService _currentUser;

    public AttendanceController(IDbConnectionFactory db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpPost("check-in")]
    public async Task<IActionResult> CheckIn([FromBody] AttendanceCheckInRequest request)
    {
        using var conn = _db.CreateConnection();
        var salesmanId = await conn.QueryFirstOrDefaultAsync<int?>("SELECT SalesmanId FROM Salesmen WHERE UserId = @UserId", new { _currentUser.UserId });
        if (!salesmanId.HasValue)
            return BadRequest(ApiResponse.Fail("Salesman profile not found."));

        var today = DateTime.UtcNow.Date;
        var existing = await conn.QueryFirstOrDefaultAsync<int?>(
            "SELECT AttendanceId FROM Attendance WHERE SalesmanId = @SalesmanId AND CAST(AttendanceDate AS DATE) = @today",
            new { SalesmanId = salesmanId.Value, today });

        if (existing.HasValue)
            return BadRequest(ApiResponse.Fail("Already checked in today."));

        var id = await conn.QueryFirstOrDefaultAsync<int>(@"
            INSERT INTO Attendance (SalesmanId, AttendanceDate, CheckInTime, CheckInLatitude, CheckInLongitude, Status, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.AttendanceId
            VALUES (@SalesmanId, @today, @CheckInTime, @Latitude, @Longitude, 'Present', GETUTCDATE(), GETUTCDATE())",
            new { SalesmanId = salesmanId.Value, today, CheckInTime = DateTime.UtcNow, request.Latitude, request.Longitude });

        return Ok(ApiResponse<object>.Ok(new { AttendanceId = id, CheckInTime = DateTime.UtcNow }, "Checked in successfully."));
    }

    [HttpPut("check-out")]
    public async Task<IActionResult> CheckOut([FromBody] AttendanceCheckOutRequest request)
    {
        using var conn = _db.CreateConnection();
        var salesmanId = await conn.QueryFirstOrDefaultAsync<int?>("SELECT SalesmanId FROM Salesmen WHERE UserId = @UserId", new { _currentUser.UserId });
        if (!salesmanId.HasValue)
            return BadRequest(ApiResponse.Fail("Salesman profile not found."));

        var today = DateTime.UtcNow.Date;
        var rows = await conn.ExecuteAsync(@"
            UPDATE Attendance SET CheckOutTime = @CheckOutTime, CheckOutLatitude = @Latitude, CheckOutLongitude = @Longitude, UpdatedAt = GETUTCDATE()
            WHERE SalesmanId = @SalesmanId AND CAST(AttendanceDate AS DATE) = @today AND CheckOutTime IS NULL",
            new { SalesmanId = salesmanId.Value, today, CheckOutTime = DateTime.UtcNow, request.Latitude, request.Longitude });

        return rows > 0 ? Ok(ApiResponse.Ok("Checked out successfully.")) : BadRequest(ApiResponse.Fail("No active check-in found."));
    }

    [HttpGet("my-attendance")]
    public async Task<IActionResult> GetMyAttendance([FromQuery] int? month, [FromQuery] int? year)
    {
        using var conn = _db.CreateConnection();
        var salesmanId = await conn.QueryFirstOrDefaultAsync<int?>("SELECT SalesmanId FROM Salesmen WHERE UserId = @UserId", new { _currentUser.UserId });
        var m = month ?? DateTime.UtcNow.Month;
        var y = year ?? DateTime.UtcNow.Year;

        var data = await conn.QueryAsync(@"
            SELECT * FROM Attendance
            WHERE SalesmanId = @SalesmanId AND MONTH(AttendanceDate) = @m AND YEAR(AttendanceDate) = @y
            ORDER BY AttendanceDate DESC", new { SalesmanId = salesmanId, m, y });

        return Ok(ApiResponse<object>.Ok(data));
    }

    [HttpGet("report")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetAttendanceReport([FromQuery] int? month, [FromQuery] int? year, [FromQuery] int? salesmanId)
    {
        using var conn = _db.CreateConnection();
        var m = month ?? DateTime.UtcNow.Month;
        var y = year ?? DateTime.UtcNow.Year;

        var where = new List<string> { "MONTH(a.AttendanceDate) = @m", "YEAR(a.AttendanceDate) = @y" };
        var p = new DynamicParameters();
        p.Add("m", m); p.Add("y", y);

        if (_currentUser.IsManager) { where.Add("sm.ManagerId = @ManagerId"); p.Add("ManagerId", _currentUser.UserId); }
        if (salesmanId.HasValue) { where.Add("a.SalesmanId = @SalesmanId"); p.Add("SalesmanId", salesmanId.Value); }

        var wc = string.Join(" AND ", where);
        var data = await conn.QueryAsync($@"
            SELECT a.*, u.FullName AS SalesmanName
            FROM Attendance a
            JOIN Salesmen sm ON a.SalesmanId = sm.SalesmanId
            JOIN Users u ON sm.UserId = u.UserId
            WHERE {wc}
            ORDER BY a.AttendanceDate DESC, u.FullName", p);

        return Ok(ApiResponse<object>.Ok(data));
    }
}

public class AttendanceCheckInRequest
{
    public string? Latitude { get; set; }
    public string? Longitude { get; set; }
    public string? Notes { get; set; }
}

public class AttendanceCheckOutRequest
{
    public string? Latitude { get; set; }
    public string? Longitude { get; set; }
}
