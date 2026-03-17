using Dapper;
using DVR.Application.Common;
using DVR.Application.DTOs.Expenses;
using DVR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVR.API.Controllers;

[ApiController]
[Route("api/expenses")]
[Authorize]
public class ExpensesController : ControllerBase
{
    private readonly IDbConnectionFactory _db;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notifications;

    public ExpensesController(IDbConnectionFactory db, ICurrentUserService currentUser, INotificationService notifications)
    {
        _db = db;
        _currentUser = currentUser;
        _notifications = notifications;
    }

    [HttpGet]
    public async Task<IActionResult> GetExpenses([FromQuery] ExpenseQueryParams query)
    {
        using var conn = _db.CreateConnection();
        var where = new List<string>();
        var p = new DynamicParameters();
        p.Add("Offset", query.Offset);
        p.Add("PageSize", query.PageSize);

        if (_currentUser.IsSalesman)
        {
            var sid = await conn.QueryFirstOrDefaultAsync<int?>("SELECT SalesmanId FROM Salesmen WHERE UserId = @UserId", new { _currentUser.UserId });
            where.Add("e.SalesmanId = @SalesmanId"); p.Add("SalesmanId", sid);
        }
        else if (_currentUser.IsManager)
        {
            where.Add("e.SalesmanId IN (SELECT SalesmanId FROM Salesmen WHERE ManagerId = @ManagerId)");
            p.Add("ManagerId", _currentUser.UserId);
        }
        else if (query.SalesmanId.HasValue) { where.Add("e.SalesmanId = @SalesmanId"); p.Add("SalesmanId", query.SalesmanId.Value); }

        if (!string.IsNullOrWhiteSpace(query.Status)) { where.Add("e.Status = @Status"); p.Add("Status", query.Status); }
        if (!string.IsNullOrWhiteSpace(query.ExpenseType)) { where.Add("e.ExpenseType = @ExpenseType"); p.Add("ExpenseType", query.ExpenseType); }
        if (query.FromDate.HasValue) { where.Add("e.ExpenseDate >= @FromDate"); p.Add("FromDate", query.FromDate.Value); }
        if (query.ToDate.HasValue) { where.Add("e.ExpenseDate <= @ToDate"); p.Add("ToDate", query.ToDate.Value.AddDays(1)); }

        var wc = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : string.Empty;
        var total = await conn.QueryFirstOrDefaultAsync<int>($"SELECT COUNT(*) FROM Expenses e {wc}", p);
        var data = await conn.QueryAsync<ExpenseDto>($@"
            SELECT e.*, u.FullName AS SalesmanName, au.FullName AS ApprovedByName
            FROM Expenses e
            JOIN Salesmen sm ON e.SalesmanId = sm.SalesmanId
            JOIN Users u ON sm.UserId = u.UserId
            LEFT JOIN Users au ON e.ApprovedById = au.UserId
            {wc} ORDER BY e.ExpenseDate DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY", p);

        return Ok(new PaginatedResponse<ExpenseDto>
        {
            Success = true, Message = "Expenses retrieved.", Data = data,
            Pagination = new PaginationMeta { Page = query.Page, PageSize = query.PageSize, TotalCount = total }
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetExpense(int id)
    {
        using var conn = _db.CreateConnection();
        var expense = await conn.QueryFirstOrDefaultAsync<ExpenseDto>(@"
            SELECT e.*, u.FullName AS SalesmanName, au.FullName AS ApprovedByName
            FROM Expenses e
            JOIN Salesmen sm ON e.SalesmanId = sm.SalesmanId
            JOIN Users u ON sm.UserId = u.UserId
            LEFT JOIN Users au ON e.ApprovedById = au.UserId
            WHERE e.ExpenseId = @id", new { id });

        return expense is not null ? Ok(ApiResponse<ExpenseDto>.Ok(expense)) : NotFound(ApiResponse<ExpenseDto>.Fail("Expense not found."));
    }

    [HttpPost]
    public async Task<IActionResult> CreateExpense([FromBody] CreateExpenseRequest request)
    {
        using var conn = _db.CreateConnection();
        var salesmanId = await conn.QueryFirstOrDefaultAsync<int?>("SELECT SalesmanId FROM Salesmen WHERE UserId = @UserId", new { _currentUser.UserId });
        if (!salesmanId.HasValue)
            return BadRequest(ApiResponse.Fail("Salesman profile not found."));

        var id = await conn.QueryFirstOrDefaultAsync<int>(@"
            INSERT INTO Expenses (SalesmanId, ExpenseReportId, ExpenseType, Amount, ExpenseDate, Description, ReceiptUrl, Status, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.ExpenseId
            VALUES (@SalesmanId, @ExpenseReportId, @ExpenseType, @Amount, @ExpenseDate, @Description, @ReceiptUrl, 'Pending', GETUTCDATE(), GETUTCDATE())",
            new { SalesmanId = salesmanId.Value, request.ExpenseReportId, request.ExpenseType, request.Amount, request.ExpenseDate, request.Description, request.ReceiptUrl });

        return Created($"/api/expenses/{id}", ApiResponse<object>.Ok(new { ExpenseId = id }, "Expense created."));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateExpense(int id, [FromBody] CreateExpenseRequest request)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(@"
            UPDATE Expenses SET
                ExpenseType = COALESCE(@ExpenseType, ExpenseType),
                Amount = COALESCE(@Amount, Amount),
                ExpenseDate = COALESCE(@ExpenseDate, ExpenseDate),
                Description = COALESCE(@Description, Description),
                ReceiptUrl = COALESCE(@ReceiptUrl, ReceiptUrl),
                UpdatedAt = GETUTCDATE()
            WHERE ExpenseId = @id AND Status = 'Pending'",
            new { request.ExpenseType, request.Amount, request.ExpenseDate, request.Description, request.ReceiptUrl, id });

        return rows > 0 ? Ok(ApiResponse.Ok("Expense updated.")) : NotFound(ApiResponse.Fail("Expense not found or already processed."));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteExpense(int id)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync("DELETE FROM Expenses WHERE ExpenseId = @id AND Status = 'Pending'", new { id });
        return rows > 0 ? Ok(ApiResponse.Ok("Expense deleted.")) : NotFound(ApiResponse.Fail("Expense not found or already processed."));
    }

    [HttpPut("{id:int}/approve")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> ApproveExpense(int id)
    {
        using var conn = _db.CreateConnection();
        var expense = await conn.QueryFirstOrDefaultAsync<DVR.Domain.Entities.Expense>("SELECT * FROM Expenses WHERE ExpenseId = @id", new { id });
        if (expense is null) return NotFound(ApiResponse.Fail("Expense not found."));

        await conn.ExecuteAsync(@"
            UPDATE Expenses SET Status = 'Approved', ApprovedById = @ApprovedById, ApprovedAt = GETUTCDATE(), UpdatedAt = GETUTCDATE()
            WHERE ExpenseId = @id", new { ApprovedById = _currentUser.UserId, id });

        var userId = await conn.QueryFirstOrDefaultAsync<int?>("SELECT UserId FROM Salesmen WHERE SalesmanId = @SalesmanId", new { expense.SalesmanId });
        if (userId.HasValue)
            await _notifications.SendToUserAsync(userId.Value, "Expense Approved", $"Your expense #{id} (₹{expense.Amount}) has been approved.");

        return Ok(ApiResponse.Ok("Expense approved."));
    }

    [HttpPut("{id:int}/reject")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> RejectExpense(int id, [FromBody] ApproveRejectRequest request)
    {
        using var conn = _db.CreateConnection();
        var expense = await conn.QueryFirstOrDefaultAsync<DVR.Domain.Entities.Expense>("SELECT * FROM Expenses WHERE ExpenseId = @id", new { id });
        if (expense is null) return NotFound(ApiResponse.Fail("Expense not found."));

        await conn.ExecuteAsync(@"
            UPDATE Expenses SET Status = 'Rejected', ApprovedById = @ApprovedById, ApprovedAt = GETUTCDATE(),
                RejectionReason = @Reason, UpdatedAt = GETUTCDATE()
            WHERE ExpenseId = @id", new { ApprovedById = _currentUser.UserId, request.Reason, id });

        var userId = await conn.QueryFirstOrDefaultAsync<int?>("SELECT UserId FROM Salesmen WHERE SalesmanId = @SalesmanId", new { expense.SalesmanId });
        if (userId.HasValue)
            await _notifications.SendToUserAsync(userId.Value, "Expense Rejected", $"Your expense #{id} was rejected. Reason: {request.Reason}");

        return Ok(ApiResponse.Ok("Expense rejected."));
    }
}
