using Dapper;
using DVR.Application.Common;
using DVR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVR.API.Controllers;

[ApiController]
[Route("api/permissions")]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly IDbConnectionFactory _db;

    public PermissionsController(IDbConnectionFactory db)
    {
        _db = db;
    }

    // GET /api/permissions/{userId}
    [HttpGet("{userId:int}")]
    public async Task<IActionResult> GetPermissions(int userId)
    {
        using var conn = _db.CreateConnection();
        var perms = await conn.QueryAsync(
            "SELECT Module, SubModule, PermLevel, CanView, CanSave, CanEdit, CanDelete, CanExport, CanPrint FROM UserModulePermissions WHERE UserId = @userId",
            new { userId });
        return Ok(ApiResponse<object>.Ok(perms));
    }

    // PUT /api/permissions/{userId}  — full replace (upsert all modules)
    [HttpPut("{userId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SavePermissions(int userId, [FromBody] List<PermissionItem> items)
    {
        using var conn = _db.CreateConnection();
        using var tx = conn.BeginTransaction();

        await conn.ExecuteAsync("DELETE FROM UserModulePermissions WHERE UserId = @userId", new { userId }, tx);

        foreach (var item in items)
        {
            await conn.ExecuteAsync(@"
                INSERT INTO UserModulePermissions (UserId, Module, SubModule, PermLevel, CanView, CanSave, CanEdit, CanDelete, CanExport, CanPrint, UpdatedAt)
                VALUES (@UserId, @Module, @SubModule, @PermLevel, @CanView, @CanSave, @CanEdit, @CanDelete, @CanExport, @CanPrint, GETUTCDATE())",
                new { UserId = userId, item.Module, item.SubModule, item.PermLevel, item.CanView, item.CanSave, item.CanEdit, item.CanDelete, item.CanExport, item.CanPrint },
                tx);
        }

        tx.Commit();
        return Ok(ApiResponse.Ok("Permissions saved."));
    }
}

public class PermissionItem
{
    public string Module { get; set; } = string.Empty;
    public string SubModule { get; set; } = string.Empty;
    public string PermLevel { get; set; } = "None";
    public bool CanView { get; set; }
    public bool CanSave { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanExport { get; set; }
    public bool CanPrint { get; set; }
}
