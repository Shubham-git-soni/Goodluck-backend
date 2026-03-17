using Dapper;
using DVR.Application.Common;
using DVR.Application.DTOs.Expenses;
using DVR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVR.API.Controllers;

[ApiController]
[Route("api/expense-reports")]
[Authorize]
public class ExpenseReportsController : ControllerBase
{
    private readonly IDbConnectionFactory _db;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notifications;

    public ExpenseReportsController(IDbConnectionFactory db, ICurrentUserService currentUser, INotificationService notifications)
    {
        _db = db;
        _currentUser = currentUser;
        _notifications = notifications;
    }

    [HttpGet]
    public async Task<IActionResult> GetReports([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
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
            where.Add("er.SalesmanId = @SalesmanId"); p.Add("SalesmanId", sid);
        }
        else if (salesmanId.HasValue) { where.Add("er.SalesmanId = @SalesmanId"); p.Add("SalesmanId", salesmanId.Value); }
        if (!string.IsNullOrWhiteSpace(status)) { where.Add("er.Status = @Status"); p.Add("Status", status); }

        var wc = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : string.Empty;
        var total = await conn.QueryFirstOrDefaultAsync<int>($"SELECT COUNT(*) FROM ExpenseReports er {wc}", p);
        var data = await conn.QueryAsync($@"
            SELECT er.*, u.FullName AS SalesmanName
            FROM ExpenseReports er
            JOIN Salesmen sm ON er.SalesmanId = sm.SalesmanId
            JOIN Users u ON sm.UserId = u.UserId
            {wc} ORDER BY er.ReportYear DESC, er.ReportMonth DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY", p);

        return Ok(new PaginatedResponse<object>
        {
            Success = true, Message = "Expense reports retrieved.", Data = data,
            Pagination = new PaginationMeta { Page = page, PageSize = pageSize, TotalCount = total }
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateReport([FromBody] CreateExpenseReportRequest request)
    {
        using var conn = _db.CreateConnection();
        var salesmanId = await conn.QueryFirstOrDefaultAsync<int?>("SELECT SalesmanId FROM Salesmen WHERE UserId = @UserId", new { _currentUser.UserId });
        if (!salesmanId.HasValue)
            return BadRequest(ApiResponse.Fail("Salesman profile not found."));

        var totalAmount = 0m;
        if (request.ExpenseIds?.Count > 0)
            totalAmount = await conn.QueryFirstOrDefaultAsync<decimal>(
                "SELECT ISNULL(SUM(Amount), 0) FROM Expenses WHERE ExpenseId IN @ExpenseIds AND SalesmanId = @SalesmanId AND Status = 'Pending'",
                new { request.ExpenseIds, SalesmanId = salesmanId.Value });

        var id = await conn.QueryFirstOrDefaultAsync<int>(@"
            INSERT INTO ExpenseReports (SalesmanId, ReportMonth, ReportYear, TotalAmount, Status, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.ExpenseReportId
            VALUES (@SalesmanId, @ReportMonth, @ReportYear, @TotalAmount, 'Draft', GETUTCDATE(), GETUTCDATE())",
            new { SalesmanId = salesmanId.Value, request.ReportMonth, request.ReportYear, TotalAmount = totalAmount });

        if (request.ExpenseIds?.Count > 0)
            await conn.ExecuteAsync("UPDATE Expenses SET ExpenseReportId = @id WHERE ExpenseId IN @ExpenseIds", new { id, request.ExpenseIds });

        return Created($"/api/expense-reports/{id}", ApiResponse<object>.Ok(new { ExpenseReportId = id }, "Expense report created."));
    }

    [HttpPut("{id:int}/submit")]
    public async Task<IActionResult> SubmitReport(int id)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(
            "UPDATE ExpenseReports SET Status = 'Submitted', UpdatedAt = GETUTCDATE() WHERE ExpenseReportId = @id AND Status = 'Draft'", new { id });

        if (rows > 0)
            await _notifications.SendToRoleAsync("Manager", "Expense Report Submitted", $"Expense report #{id} has been submitted for approval.", "ExpenseReport");

        return rows > 0 ? Ok(ApiResponse.Ok("Expense report submitted.")) : NotFound(ApiResponse.Fail("Report not found or already submitted."));
    }

    [HttpPut("{id:int}/approve")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> ApproveReport(int id)
    {
        using var conn = _db.CreateConnection();
        var report = await conn.QueryFirstOrDefaultAsync<dynamic>("SELECT * FROM ExpenseReports WHERE ExpenseReportId = @id", new { id });
        if (report is null) return NotFound(ApiResponse.Fail("Report not found."));

        await conn.ExecuteAsync(@"
            UPDATE ExpenseReports SET Status = 'Approved', ApprovedById = @ApprovedById, ApprovedAt = GETUTCDATE(), UpdatedAt = GETUTCDATE()
            WHERE ExpenseReportId = @id", new { ApprovedById = _currentUser.UserId, id });

        await conn.ExecuteAsync("UPDATE Expenses SET Status = 'Approved', ApprovedById = @ApprovedById, ApprovedAt = GETUTCDATE() WHERE ExpenseReportId = @id",
            new { ApprovedById = _currentUser.UserId, id });

        return Ok(ApiResponse.Ok("Expense report approved."));
    }
}
