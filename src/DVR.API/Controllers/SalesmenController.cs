using Dapper;
using DVR.Application.Common;
using DVR.Application.DTOs.Salesmen;
using DVR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVR.API.Controllers;

[ApiController]
[Route("api/salesmen")]
[Authorize]
public class SalesmenController : ControllerBase
{
    private readonly IDbConnectionFactory _db;
    private readonly ICurrentUserService _currentUser;

    public SalesmenController(IDbConnectionFactory db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetSalesmen([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
    {
        using var conn = _db.CreateConnection();
        var where = new List<string> { "sm.IsActive = 1" };
        var parameters = new DynamicParameters();
        parameters.Add("Offset", (page - 1) * pageSize);
        parameters.Add("PageSize", pageSize);

        if (!string.IsNullOrWhiteSpace(search))
        {
            where.Add("(u.FullName LIKE @Search OR sm.EmployeeCode LIKE @Search OR u.Email LIKE @Search)");
            parameters.Add("Search", $"%{search}%");
        }

        if (_currentUser.IsManager)
        {
            where.Add("sm.ManagerId = @ManagerId");
            parameters.Add("ManagerId", _currentUser.UserId);
        }

        var whereClause = string.Join(" AND ", where);
        var total = await conn.QueryFirstOrDefaultAsync<int>($"SELECT COUNT(*) FROM Salesmen sm JOIN Users u ON sm.UserId = u.UserId WHERE {whereClause}", parameters);
        var data = await conn.QueryAsync<SalesmanDto>($@"
            SELECT sm.SalesmanId, sm.UserId, sm.EmployeeCode, u.Username, u.FullName, u.Email, u.Phone,
                sm.Territory, sm.Zone, sm.State, sm.City, sm.ManagerId,
                mu.FullName AS ManagerName, sm.Designation, sm.JoiningDate, sm.IsActive, sm.CreatedAt
            FROM Salesmen sm
            JOIN Users u ON sm.UserId = u.UserId
            LEFT JOIN Users mu ON sm.ManagerId = mu.UserId
            WHERE {whereClause}
            ORDER BY u.FullName
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY", parameters);

        return Ok(new DVR.Application.Common.PaginatedResponse<SalesmanDto>
        {
            Success = true,
            Message = "Salesmen retrieved.",
            Data = data,
            Pagination = new DVR.Application.Common.PaginationMeta { Page = page, PageSize = pageSize, TotalCount = total }
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetSalesman(int id)
    {
        using var conn = _db.CreateConnection();
        var salesman = await conn.QueryFirstOrDefaultAsync<SalesmanDto>(@"
            SELECT sm.SalesmanId, sm.UserId, sm.EmployeeCode, u.Username, u.FullName, u.Email, u.Phone,
                sm.Territory, sm.Zone, sm.State, sm.City, sm.ManagerId,
                mu.FullName AS ManagerName, sm.Designation, sm.JoiningDate, sm.IsActive, sm.CreatedAt
            FROM Salesmen sm
            JOIN Users u ON sm.UserId = u.UserId
            LEFT JOIN Users mu ON sm.ManagerId = mu.UserId
            WHERE sm.SalesmanId = @id", new { id });

        return salesman is not null
            ? Ok(ApiResponse<SalesmanDto>.Ok(salesman))
            : NotFound(ApiResponse<SalesmanDto>.Fail("Salesman not found."));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> CreateSalesman([FromBody] CreateSalesmanRequest request)
    {
        using var conn = _db.CreateConnection();
        var existingUser = await conn.QueryFirstOrDefaultAsync<int?>(
            "SELECT UserId FROM Users WHERE Username = @Username", new { request.Username });
        if (existingUser.HasValue)
            return BadRequest(ApiResponse.Fail("Username already exists."));

        var userId = await conn.QueryFirstOrDefaultAsync<int>(@"
            INSERT INTO Users (Username, PasswordHash, FullName, Email, Phone, Role, ManagerId, IsActive, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.UserId
            VALUES (@Username, @Password, @FullName, @Email, @Phone, 3, @ManagerId, 1, GETUTCDATE(), GETUTCDATE())",
            new { request.Username, Password = request.Password, request.FullName, request.Email, request.Phone, ManagerId = request.ManagerId });

        var salesmanId = await conn.QueryFirstOrDefaultAsync<int>(@"
            INSERT INTO Salesmen (UserId, EmployeeCode, Territory, Zone, State, City, ManagerId, Designation, JoiningDate, IsActive, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.SalesmanId
            VALUES (@UserId, @EmployeeCode, @Territory, @Zone, @State, @City, @ManagerId, @Designation, @JoiningDate, 1, GETUTCDATE(), GETUTCDATE())",
            new { UserId = userId, request.EmployeeCode, request.Territory, request.Zone, request.State, request.City, request.ManagerId, request.Designation, request.JoiningDate });

        return Created($"/api/salesmen/{salesmanId}", ApiResponse<object>.Ok(new { salesmanId, userId }, "Salesman created."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpdateSalesman(int id, [FromBody] UpdateSalesmanRequest request)
    {
        using var conn = _db.CreateConnection();
        var salesman = await conn.QueryFirstOrDefaultAsync<DVR.Domain.Entities.Salesman>(
            "SELECT * FROM Salesmen WHERE SalesmanId = @id", new { id });

        if (salesman is null)
            return NotFound(ApiResponse.Fail("Salesman not found."));

        await conn.ExecuteAsync(@"
            UPDATE Salesmen SET
                Territory = COALESCE(@Territory, Territory),
                Zone = COALESCE(@Zone, Zone),
                State = COALESCE(@State, State),
                City = COALESCE(@City, City),
                ManagerId = COALESCE(@ManagerId, ManagerId),
                Designation = COALESCE(@Designation, Designation),
                JoiningDate = COALESCE(@JoiningDate, JoiningDate),
                IsActive = COALESCE(@IsActive, IsActive),
                UpdatedAt = GETUTCDATE()
            WHERE SalesmanId = @id", new { request.Territory, request.Zone, request.State, request.City, request.ManagerId, request.Designation, request.JoiningDate, request.IsActive, id });

        await conn.ExecuteAsync(@"
            UPDATE Users SET
                FullName = COALESCE(@FullName, FullName),
                Email = COALESCE(@Email, Email),
                Phone = COALESCE(@Phone, Phone),
                IsActive = COALESCE(@IsActive, IsActive),
                UpdatedAt = GETUTCDATE()
            WHERE UserId = @UserId", new { request.FullName, request.Email, request.Phone, request.IsActive, salesman.UserId });

        return Ok(ApiResponse.Ok("Salesman updated."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteSalesman(int id)
    {
        using var conn = _db.CreateConnection();
        var salesman = await conn.QueryFirstOrDefaultAsync<DVR.Domain.Entities.Salesman>(
            "SELECT * FROM Salesmen WHERE SalesmanId = @id AND IsActive = 1", new { id });

        if (salesman is null)
            return NotFound(ApiResponse.Fail("Salesman not found."));

        await conn.ExecuteAsync(
            "UPDATE Salesmen SET IsActive = 0, UpdatedAt = GETUTCDATE() WHERE SalesmanId = @id", new { id });

        await conn.ExecuteAsync(
            "UPDATE Users SET IsActive = 0, UpdatedAt = GETUTCDATE() WHERE UserId = @UserId", new { salesman.UserId });

        return Ok(ApiResponse.Ok("Salesman deleted."));
    }

    [HttpGet("{id:int}/performance")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetPerformance(int id, [FromQuery] int? month, [FromQuery] int? year)
    {
        using var conn = _db.CreateConnection();
        var m = month ?? DateTime.UtcNow.Month;
        var y = year ?? DateTime.UtcNow.Year;

        var perf = await conn.QueryFirstOrDefaultAsync<SalesmanPerformanceDto>(@"
            SELECT sm.SalesmanId, u.FullName,
                COUNT(DISTINCT v.VisitId) AS TotalVisits,
                COUNT(DISTINCT CASE WHEN v.VisitType = 1 THEN v.VisitId END) AS SchoolVisits,
                COUNT(DISTINCT CASE WHEN v.VisitType = 2 THEN v.VisitId END) AS BooksellerVisits,
                (SELECT COUNT(*) FROM Schools WHERE AssignedSalesmanId = sm.SalesmanId AND IsActive = 1) AS TotalSchools,
                (SELECT COUNT(*) FROM BookSellers WHERE AssignedSalesmanId = sm.SalesmanId AND IsActive = 1) AS TotalBooksellers,
                COUNT(DISTINCT CASE WHEN a.Status = 'Present' THEN a.AttendanceId END) AS PresentDays,
                COUNT(DISTINCT CASE WHEN a.Status = 'Absent' THEN a.AttendanceId END) AS AbsentDays,
                ISNULL(SUM(DISTINCT e.Amount), 0) AS TotalExpenses,
                (SELECT COUNT(*) FROM TourPlans WHERE SalesmanId = sm.SalesmanId AND Status = 'Submitted') AS PendingTourPlans,
                (SELECT COUNT(*) FROM TourPlans WHERE SalesmanId = sm.SalesmanId AND Status = 'Approved') AS ApprovedTourPlans
            FROM Salesmen sm
            JOIN Users u ON sm.UserId = u.UserId
            LEFT JOIN Visits v ON sm.SalesmanId = v.SalesmanId AND MONTH(v.VisitDate) = @m AND YEAR(v.VisitDate) = @y
            LEFT JOIN Attendance a ON sm.SalesmanId = a.SalesmanId AND MONTH(a.AttendanceDate) = @m AND YEAR(a.AttendanceDate) = @y
            LEFT JOIN Expenses e ON sm.SalesmanId = e.SalesmanId AND MONTH(e.ExpenseDate) = @m AND YEAR(e.ExpenseDate) = @y
            WHERE sm.SalesmanId = @id
            GROUP BY sm.SalesmanId, u.FullName", new { id, m, y });

        return perf is not null
            ? Ok(ApiResponse<SalesmanPerformanceDto>.Ok(perf))
            : NotFound(ApiResponse.Fail("Salesman not found."));
    }
}
