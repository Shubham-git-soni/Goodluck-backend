using Dapper;
using DVR.Application.Common;
using DVR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVR.API.Controllers;

[ApiController]
[Route("api/dropdowns")]
[Authorize]
public class DropdownsController : ControllerBase
{
    private readonly IDbConnectionFactory _db;

    public DropdownsController(IDbConnectionFactory db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        using var conn = _db.CreateConnection();
        var data = await conn.QueryAsync<DropdownItemDto>(
            "SELECT DropdownId, Category, Value, Label, SortOrder FROM Dropdowns WHERE IsActive = 1 ORDER BY Category, SortOrder, Label");

        var grouped = data.GroupBy(x => x.Category)
            .ToDictionary(g => g.Key, g => g.ToList());

        return Ok(ApiResponse<object>.Ok(grouped));
    }

    [HttpGet("{category}")]
    public async Task<IActionResult> GetByCategory(string category)
    {
        using var conn = _db.CreateConnection();
        var data = await conn.QueryAsync<DropdownItemDto>(
            "SELECT DropdownId, Category, Value, Label, SortOrder FROM Dropdowns WHERE Category = @category AND IsActive = 1 ORDER BY SortOrder, Label",
            new { category });
        return Ok(ApiResponse<object>.Ok(data));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateDropdown([FromBody] CreateDropdownRequest request)
    {
        using var conn = _db.CreateConnection();
        var id = await conn.QueryFirstOrDefaultAsync<int>(@"
            INSERT INTO Dropdowns (Category, Value, Label, SortOrder, IsActive, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.DropdownId
            VALUES (@Category, @Value, @Label, @SortOrder, 1, GETUTCDATE(), GETUTCDATE())", request);

        return Created($"/api/dropdowns/{id}", ApiResponse<object>.Ok(new { DropdownId = id }, "Dropdown item created."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateDropdown(int id, [FromBody] CreateDropdownRequest request)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(@"
            UPDATE Dropdowns SET Category = @Category, Value = @Value, Label = @Label,
                SortOrder = @SortOrder, UpdatedAt = GETUTCDATE()
            WHERE DropdownId = @id",
            new { request.Category, request.Value, request.Label, request.SortOrder, id });

        return rows > 0 ? Ok(ApiResponse.Ok("Dropdown updated.")) : NotFound(ApiResponse.Fail("Dropdown not found."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteDropdown(int id)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync("UPDATE Dropdowns SET IsActive = 0, UpdatedAt = GETUTCDATE() WHERE DropdownId = @id", new { id });
        return rows > 0 ? Ok(ApiResponse.Ok("Dropdown deleted.")) : NotFound(ApiResponse.Fail("Dropdown not found."));
    }
}

public class CreateDropdownRequest
{
    public string Category { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class DropdownItemDto
{
    public int DropdownId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
