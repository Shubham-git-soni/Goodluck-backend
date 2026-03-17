using Dapper;
using DVR.Application.Common;
using DVR.Application.DTOs.Expenses;
using DVR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVR.API.Controllers;

[ApiController]
[Route("api/expense-policies")]
[Authorize]
public class ExpensePoliciesController : ControllerBase
{
    private readonly IDbConnectionFactory _db;

    public ExpensePoliciesController(IDbConnectionFactory db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetPolicies()
    {
        using var conn = _db.CreateConnection();
        var data = await conn.QueryAsync<ExpensePolicyDto>("SELECT * FROM ExpensePolicies WHERE IsActive = 1 ORDER BY ExpenseType");
        return Ok(ApiResponse<object>.Ok(data));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreatePolicy([FromBody] CreateExpensePolicyRequest request)
    {
        using var conn = _db.CreateConnection();
        var id = await conn.QueryFirstOrDefaultAsync<int>(@"
            INSERT INTO ExpensePolicies (PolicyName, ExpenseType, MaxAmount, ApplicableRole, IsActive, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.PolicyId
            VALUES (@PolicyName, @ExpenseType, @MaxAmount, @ApplicableRole, 1, GETUTCDATE(), GETUTCDATE())",
            request);
        return Created($"/api/expense-policies/{id}", ApiResponse<object>.Ok(new { PolicyId = id }, "Policy created."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdatePolicy(int id, [FromBody] CreateExpensePolicyRequest request)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(@"
            UPDATE ExpensePolicies SET PolicyName = @PolicyName, ExpenseType = @ExpenseType,
                MaxAmount = @MaxAmount, ApplicableRole = @ApplicableRole, UpdatedAt = GETUTCDATE()
            WHERE PolicyId = @id", new { request.PolicyName, request.ExpenseType, request.MaxAmount, request.ApplicableRole, id });

        return rows > 0 ? Ok(ApiResponse.Ok("Policy updated.")) : NotFound(ApiResponse.Fail("Policy not found."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeletePolicy(int id)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync("UPDATE ExpensePolicies SET IsActive = 0, UpdatedAt = GETUTCDATE() WHERE PolicyId = @id", new { id });
        return rows > 0 ? Ok(ApiResponse.Ok("Policy deleted.")) : NotFound(ApiResponse.Fail("Policy not found."));
    }
}
