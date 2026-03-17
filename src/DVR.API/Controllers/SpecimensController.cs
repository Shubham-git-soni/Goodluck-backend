using Dapper;
using DVR.Application.Common;
using DVR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVR.API.Controllers;

[ApiController]
[Route("api/specimens")]
[Authorize]
public class SpecimensController : ControllerBase
{
    private readonly IDbConnectionFactory _db;
    private readonly ICurrentUserService _currentUser;

    public SpecimensController(IDbConnectionFactory db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetSpecimens([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null, [FromQuery] int? salesmanId = null, [FromQuery] int? bookId = null)
    {
        using var conn = _db.CreateConnection();
        var where = new List<string>();
        var p = new DynamicParameters();
        p.Add("Offset", (page - 1) * pageSize);
        p.Add("PageSize", pageSize);

        if (_currentUser.IsSalesman)
        {
            var sid = await conn.QueryFirstOrDefaultAsync<int?>("SELECT SalesmanId FROM Salesmen WHERE UserId = @UserId", new { _currentUser.UserId });
            where.Add("sp.SalesmanId = @SalesmanId"); p.Add("SalesmanId", sid);
        }
        else if (salesmanId.HasValue) { where.Add("sp.SalesmanId = @SalesmanId"); p.Add("SalesmanId", salesmanId.Value); }
        if (!string.IsNullOrWhiteSpace(status)) { where.Add("sp.Status = @Status"); p.Add("Status", status); }
        if (bookId.HasValue) { where.Add("sp.BookId = @BookId"); p.Add("BookId", bookId.Value); }

        var wc = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : string.Empty;
        var total = await conn.QueryFirstOrDefaultAsync<int>($"SELECT COUNT(*) FROM Specimens sp {wc}", p);
        var data = await conn.QueryAsync($@"
            SELECT sp.*, b.Title AS BookTitle, u.FullName AS SalesmanName,
                s.SchoolName, sc.Name AS TeacherName
            FROM Specimens sp
            JOIN Books b ON sp.BookId = b.BookId
            JOIN Salesmen sm ON sp.SalesmanId = sm.SalesmanId
            JOIN Users u ON sm.UserId = u.UserId
            LEFT JOIN Schools s ON sp.SchoolId = s.SchoolId
            LEFT JOIN SchoolContacts sc ON sp.TeacherContactId = sc.ContactId
            {wc} ORDER BY sp.CreatedAt DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY", p);

        return Ok(new PaginatedResponse<object>
        {
            Success = true, Message = "Specimens retrieved.", Data = data,
            Pagination = new PaginationMeta { Page = page, PageSize = pageSize, TotalCount = total }
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetSpecimen(int id)
    {
        using var conn = _db.CreateConnection();
        var specimen = await conn.QueryFirstOrDefaultAsync(@"
            SELECT sp.*, b.Title AS BookTitle, u.FullName AS SalesmanName, s.SchoolName, sc.Name AS TeacherName
            FROM Specimens sp
            JOIN Books b ON sp.BookId = b.BookId
            JOIN Salesmen sm ON sp.SalesmanId = sm.SalesmanId
            JOIN Users u ON sm.UserId = u.UserId
            LEFT JOIN Schools s ON sp.SchoolId = s.SchoolId
            LEFT JOIN SchoolContacts sc ON sp.TeacherContactId = sc.ContactId
            WHERE sp.SpecimenId = @id", new { id });

        return specimen is not null ? Ok(ApiResponse<object>.Ok(specimen)) : NotFound(ApiResponse<object>.Fail("Specimen not found."));
    }

    [HttpPost]
    public async Task<IActionResult> CreateSpecimen([FromBody] CreateSpecimenRequest request)
    {
        using var conn = _db.CreateConnection();
        var salesmanId = await conn.QueryFirstOrDefaultAsync<int?>("SELECT SalesmanId FROM Salesmen WHERE UserId = @UserId", new { _currentUser.UserId });
        if (!salesmanId.HasValue && !_currentUser.IsAdmin)
            return BadRequest(ApiResponse.Fail("Salesman profile not found."));

        var sid = salesmanId ?? request.SalesmanId;
        var id = await conn.QueryFirstOrDefaultAsync<int>(@"
            INSERT INTO Specimens (BookId, SalesmanId, SchoolId, TeacherContactId, Status, Notes, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.SpecimenId
            VALUES (@BookId, @SalesmanId, @SchoolId, @TeacherContactId, 'Pending', @Notes, GETUTCDATE(), GETUTCDATE())",
            new { request.BookId, SalesmanId = sid, request.SchoolId, request.TeacherContactId, request.Notes });

        return Created($"/api/specimens/{id}", ApiResponse<object>.Ok(new { SpecimenId = id }, "Specimen created."));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateSpecimen(int id, [FromBody] CreateSpecimenRequest request)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(@"
            UPDATE Specimens SET
                SchoolId = COALESCE(@SchoolId, SchoolId),
                TeacherContactId = COALESCE(@TeacherContactId, TeacherContactId),
                Notes = COALESCE(@Notes, Notes),
                UpdatedAt = GETUTCDATE()
            WHERE SpecimenId = @id",
            new { request.SchoolId, request.TeacherContactId, request.Notes, id });

        return rows > 0 ? Ok(ApiResponse.Ok("Specimen updated.")) : NotFound(ApiResponse.Fail("Specimen not found."));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteSpecimen(int id)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync("DELETE FROM Specimens WHERE SpecimenId = @id", new { id });
        return rows > 0 ? Ok(ApiResponse.Ok("Specimen deleted.")) : NotFound(ApiResponse.Fail("Specimen not found."));
    }

    [HttpPut("{id:int}/allocate")]
    public async Task<IActionResult> AllocateSpecimen(int id, [FromBody] AllocateSpecimenRequest request)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(@"
            UPDATE Specimens SET Status = 'Allocated', AllocatedDate = GETUTCDATE(),
                RecipientName = @RecipientName, SchoolId = COALESCE(@SchoolId, SchoolId),
                TeacherContactId = COALESCE(@TeacherContactId, TeacherContactId),
                UpdatedAt = GETUTCDATE()
            WHERE SpecimenId = @id",
            new { request.RecipientName, request.SchoolId, request.TeacherContactId, id });

        return rows > 0 ? Ok(ApiResponse.Ok("Specimen allocated.")) : NotFound(ApiResponse.Fail("Specimen not found."));
    }
}

public class CreateSpecimenRequest
{
    public int BookId { get; set; }
    public int? SalesmanId { get; set; }
    public int? SchoolId { get; set; }
    public int? TeacherContactId { get; set; }
    public string? Notes { get; set; }
}

public class AllocateSpecimenRequest
{
    public string? RecipientName { get; set; }
    public int? SchoolId { get; set; }
    public int? TeacherContactId { get; set; }
}
