using Dapper;
using DVR.Application.Common;
using DVR.Application.Interfaces;
using DVR.Domain.Entities;
using DVR.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVR.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly IDbConnectionFactory _db;

    public UsersController(IDbConnectionFactory db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null, [FromQuery] string? role = null)
    {
        using var conn = _db.CreateConnection();
        var where = new List<string> { "IsActive = 1" };
        var p = new DynamicParameters();
        p.Add("Offset", (page - 1) * pageSize);
        p.Add("PageSize", pageSize);

        if (!string.IsNullOrWhiteSpace(search)) { where.Add("(FullName LIKE @Search OR Username LIKE @Search OR Email LIKE @Search)"); p.Add("Search", $"%{search}%"); }
        if (!string.IsNullOrWhiteSpace(role)) { where.Add("Role = @Role"); p.Add("Role", role); }

        var wc = string.Join(" AND ", where);
        var total = await conn.QueryFirstOrDefaultAsync<int>($"SELECT COUNT(*) FROM Users WHERE {wc}", p);
        var data = await conn.QueryAsync($@"
            SELECT UserId, Username, FullName, Email, Phone, Role, ManagerId, IsActive, ProfilePhotoUrl, CreatedAt
            FROM Users WHERE {wc} ORDER BY FullName
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY", p);

        return Ok(new PaginatedResponse<object>
        {
            Success = true, Message = "Users retrieved.", Data = data,
            Pagination = new PaginationMeta { Page = page, PageSize = pageSize, TotalCount = total }
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetUser(int id)
    {
        using var conn = _db.CreateConnection();
        var user = await conn.QueryFirstOrDefaultAsync(
            "SELECT UserId, Username, FullName, Email, Phone, Role, ManagerId, IsActive, ProfilePhotoUrl, CreatedAt FROM Users WHERE UserId = @id", new { id });

        return user is not null ? Ok(ApiResponse<object>.Ok(user)) : NotFound(ApiResponse<object>.Fail("User not found."));
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        using var conn = _db.CreateConnection();
        var existing = await conn.QueryFirstOrDefaultAsync<int?>("SELECT UserId FROM Users WHERE Username = @Username", new { request.Username });
        if (existing.HasValue)
            return BadRequest(ApiResponse.Fail("Username already exists."));

        var id = await conn.QueryFirstOrDefaultAsync<int>(@"
            INSERT INTO Users (Username, PasswordHash, FullName, Email, Phone, Role, ManagerId, IsActive, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.UserId
            VALUES (@Username, @Password, @FullName, @Email, @Phone, @Role, @ManagerId, 1, GETUTCDATE(), GETUTCDATE())",
            new { request.Username, Password = request.Password, request.FullName, request.Email, request.Phone, request.Role, request.ManagerId });

        return Created($"/api/users/{id}", ApiResponse<object>.Ok(new { UserId = id }, "User created."));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(@"
            UPDATE Users SET
                FullName = COALESCE(@FullName, FullName),
                Email = COALESCE(@Email, Email),
                Phone = COALESCE(@Phone, Phone),
                Role = COALESCE(@Role, Role),
                ManagerId = COALESCE(@ManagerId, ManagerId),
                IsActive = COALESCE(@IsActive, IsActive),
                UpdatedAt = GETUTCDATE()
            WHERE UserId = @id",
            new { request.FullName, request.Email, request.Phone, request.Role, request.ManagerId, request.IsActive, id });

        return rows > 0 ? Ok(ApiResponse.Ok("User updated.")) : NotFound(ApiResponse.Fail("User not found."));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync("UPDATE Users SET IsActive = 0, UpdatedAt = GETUTCDATE() WHERE UserId = @id", new { id });
        return rows > 0 ? Ok(ApiResponse.Ok("User deleted.")) : NotFound(ApiResponse.Fail("User not found."));
    }

    [HttpGet("{id:int}/permissions")]
    public async Task<IActionResult> GetPermissions(int id)
    {
        using var conn = _db.CreateConnection();
        var perms = await conn.QueryAsync<UserModulePermission>("SELECT * FROM UserModulePermissions WHERE UserId = @id", new { id });
        return Ok(ApiResponse<object>.Ok(perms));
    }

    [HttpPut("{id:int}/permissions")]
    public async Task<IActionResult> UpdatePermissions(int id, [FromBody] List<UpdatePermissionRequest> permissions)
    {
        using var conn = _db.CreateConnection();
        foreach (var perm in permissions)
        {
            await conn.ExecuteAsync(@"
                MERGE UserModulePermissions AS target
                USING (SELECT @UserId AS UserId, @ModuleName AS ModuleName, @PermLevel AS PermLevel) AS source
                ON target.UserId = source.UserId AND target.ModuleName = source.ModuleName
                WHEN MATCHED THEN UPDATE SET PermLevel = source.PermLevel, UpdatedAt = GETUTCDATE()
                WHEN NOT MATCHED THEN INSERT (UserId, ModuleName, PermLevel, CreatedAt, UpdatedAt)
                    VALUES (source.UserId, source.ModuleName, source.PermLevel, GETUTCDATE(), GETUTCDATE());",
                new { UserId = id, perm.ModuleName, perm.PermLevel });
        }
        return Ok(ApiResponse.Ok("Permissions updated."));
    }
}

public class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int Role { get; set; } = 3;
    public int? ManagerId { get; set; }
}

public class UpdateUserRequest
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public int? Role { get; set; }
    public int? ManagerId { get; set; }
    public bool? IsActive { get; set; }
}

public class UpdatePermissionRequest
{
    public string ModuleName { get; set; } = string.Empty;
    public int PermLevel { get; set; }
}
