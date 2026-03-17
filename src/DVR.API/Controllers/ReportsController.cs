using Dapper;
using DVR.Application.Common;
using DVR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVR.API.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Admin,Manager")]
public class ReportsController : ControllerBase
{
    private readonly IDbConnectionFactory _db;
    private readonly ICurrentUserService _currentUser;

    public ReportsController(IDbConnectionFactory db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet("attendance")]
    public async Task<IActionResult> AttendanceReport([FromQuery] int? month, [FromQuery] int? year, [FromQuery] int? salesmanId)
    {
        using var conn = _db.CreateConnection();
        var m = month ?? DateTime.UtcNow.Month;
        var y = year ?? DateTime.UtcNow.Year;

        var data = await conn.QueryAsync(@"
            SELECT sm.SalesmanId, u.FullName,
                COUNT(CASE WHEN a.Status = 'Present' THEN 1 END) AS PresentDays,
                COUNT(CASE WHEN a.Status = 'Absent' THEN 1 END) AS AbsentDays,
                COUNT(CASE WHEN a.Status = 'Leave' THEN 1 END) AS LeaveDays,
                COUNT(a.AttendanceId) AS TotalDays,
                AVG(DATEDIFF(MINUTE, a.CheckInTime, COALESCE(a.CheckOutTime, a.CheckInTime))) AS AvgMinutesWorked
            FROM Salesmen sm
            JOIN Users u ON sm.UserId = u.UserId
            LEFT JOIN Attendance a ON sm.SalesmanId = a.SalesmanId AND MONTH(a.AttendanceDate) = @m AND YEAR(a.AttendanceDate) = @y
            WHERE sm.IsActive = 1
            GROUP BY sm.SalesmanId, u.FullName
            ORDER BY u.FullName", new { m, y });

        return Ok(ApiResponse<object>.Ok(data));
    }

    [HttpGet("visits")]
    public async Task<IActionResult> VisitsReport([FromQuery] int? month, [FromQuery] int? year, [FromQuery] int? salesmanId)
    {
        using var conn = _db.CreateConnection();
        var m = month ?? DateTime.UtcNow.Month;
        var y = year ?? DateTime.UtcNow.Year;

        var data = await conn.QueryAsync(@"
            SELECT sm.SalesmanId, u.FullName,
                COUNT(v.VisitId) AS TotalVisits,
                COUNT(CASE WHEN v.VisitType = 1 THEN 1 END) AS SchoolVisits,
                COUNT(CASE WHEN v.VisitType = 2 THEN 1 END) AS BooksellerVisits,
                COUNT(CASE WHEN v.IsCompleted = 1 THEN 1 END) AS CompletedVisits
            FROM Salesmen sm
            JOIN Users u ON sm.UserId = u.UserId
            LEFT JOIN Visits v ON sm.SalesmanId = v.SalesmanId AND MONTH(v.VisitDate) = @m AND YEAR(v.VisitDate) = @y
            WHERE sm.IsActive = 1
            GROUP BY sm.SalesmanId, u.FullName
            ORDER BY TotalVisits DESC", new { m, y });

        return Ok(ApiResponse<object>.Ok(data));
    }

    [HttpGet("year-comparison")]
    public async Task<IActionResult> YearComparison([FromQuery] int? year)
    {
        using var conn = _db.CreateConnection();
        var y = year ?? DateTime.UtcNow.Year;

        var data = await conn.QueryAsync(@"
            SELECT MONTH(VisitDate) AS Month,
                COUNT(*) AS TotalVisits,
                COUNT(CASE WHEN VisitType = 1 THEN 1 END) AS SchoolVisits,
                COUNT(CASE WHEN VisitType = 2 THEN 1 END) AS BooksellerVisits
            FROM Visits
            WHERE YEAR(VisitDate) = @y
            GROUP BY MONTH(VisitDate)
            ORDER BY Month", new { y });

        return Ok(ApiResponse<object>.Ok(data));
    }

    [HttpGet("school-analytics")]
    public async Task<IActionResult> SchoolAnalytics([FromQuery] string? state, [FromQuery] string? city)
    {
        using var conn = _db.CreateConnection();
        var data = await conn.QueryAsync(@"
            SELECT s.State, s.City, s.Board, s.SchoolType, s.Category,
                COUNT(*) AS TotalSchools,
                SUM(s.TotalStudents) AS TotalStudents,
                COUNT(v.VisitId) AS TotalVisits
            FROM Schools s
            LEFT JOIN Visits v ON s.SchoolId = v.SchoolId
            WHERE s.IsActive = 1
            GROUP BY s.State, s.City, s.Board, s.SchoolType, s.Category
            ORDER BY s.State, s.City");

        return Ok(ApiResponse<object>.Ok(data));
    }

    [HttpGet("prescribed-books")]
    public async Task<IActionResult> PrescribedBooksReport()
    {
        using var conn = _db.CreateConnection();
        var data = await conn.QueryAsync(@"
            SELECT b.BookId, b.Title, b.Author, b.Subject, b.Series,
                COUNT(spb.Id) AS SchoolsCount,
                SUM(spb.Quantity) AS TotalQuantity
            FROM Books b
            LEFT JOIN SchoolPrescribedBooks spb ON b.BookId = spb.BookId
            WHERE b.IsActive = 1
            GROUP BY b.BookId, b.Title, b.Author, b.Subject, b.Series
            ORDER BY SchoolsCount DESC");

        return Ok(ApiResponse<object>.Ok(data));
    }

    [HttpGet("specimen-tracking")]
    public async Task<IActionResult> SpecimenTrackingReport()
    {
        using var conn = _db.CreateConnection();
        var data = await conn.QueryAsync(@"
            SELECT sp.SpecimenId, b.Title AS BookTitle, u.FullName AS SalesmanName,
                s.SchoolName, sc.Name AS RecipientName, sp.Status, sp.AllocatedDate, sp.DeliveredDate
            FROM Specimens sp
            JOIN Books b ON sp.BookId = b.BookId
            JOIN Salesmen sm ON sp.SalesmanId = sm.SalesmanId
            JOIN Users u ON sm.UserId = u.UserId
            LEFT JOIN Schools s ON sp.SchoolId = s.SchoolId
            LEFT JOIN SchoolContacts sc ON sp.TeacherContactId = sc.ContactId
            ORDER BY sp.CreatedAt DESC");

        return Ok(ApiResponse<object>.Ok(data));
    }
}
