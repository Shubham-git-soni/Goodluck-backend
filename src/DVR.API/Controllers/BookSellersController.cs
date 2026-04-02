using Dapper;
using DVR.Application.Common;
using DVR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVR.API.Controllers;

[ApiController]
[Route("api/booksellers")]
[Authorize]
public class BookSellersController : ControllerBase
{
    private readonly IDbConnectionFactory _db;
    private readonly ICurrentUserService _currentUser;

    public BookSellersController(IDbConnectionFactory db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetBookSellers([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, [FromQuery] string? city = null, [FromQuery] string? state = null, [FromQuery] int? salesmanId = null)
    {
        using var conn = _db.CreateConnection();
        var where = new List<string> { "b.IsActive = 1" };
        var p = new DynamicParameters();
        p.Add("Offset", (page - 1) * pageSize);
        p.Add("PageSize", pageSize);

        if (!string.IsNullOrWhiteSpace(search))
        {
            where.Add("(b.ShopName LIKE @Search OR b.OwnerName LIKE @Search)");
            p.Add("Search", $"%{search}%");
        }
        if (!string.IsNullOrWhiteSpace(city)) { where.Add("b.City = @City"); p.Add("City", city); }
        if (!string.IsNullOrWhiteSpace(state)) { where.Add("b.State = @State"); p.Add("State", state); }

        if (_currentUser.IsSalesman)
        {
            // Salesmen see all active booksellers (master list for field visits)
        }
        else if (_currentUser.IsManager)
        {
            where.Add("b.AssignedSalesmanId IN (SELECT SalesmanId FROM Salesmen WHERE ManagerId = @ManagerId)");
            p.Add("ManagerId", _currentUser.UserId);
        }
        else if (salesmanId.HasValue) { where.Add("b.AssignedSalesmanId = @SalesmanId"); p.Add("SalesmanId", salesmanId.Value); }

        var wc = string.Join(" AND ", where);
        var total = await conn.QueryFirstOrDefaultAsync<int>($"SELECT COUNT(*) FROM BookSellers b WHERE {wc}", p);
        var data = await conn.QueryAsync($@"
            SELECT b.*, u.FullName AS AssignedSalesmanName
            FROM BookSellers b
            LEFT JOIN Salesmen sm ON b.AssignedSalesmanId = sm.SalesmanId
            LEFT JOIN Users u ON sm.UserId = u.UserId
            WHERE {wc}
            ORDER BY b.ShopName
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY", p);

        return Ok(new PaginatedResponse<object>
        {
            Success = true, Message = "Booksellers retrieved.", Data = data,
            Pagination = new PaginationMeta { Page = page, PageSize = pageSize, TotalCount = total }
        });
    }

    [HttpGet("my-booksellers")]
    public async Task<IActionResult> GetMyBookSellers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        using var conn = _db.CreateConnection();
        var sid = await conn.QueryFirstOrDefaultAsync<int?>("SELECT SalesmanId FROM Salesmen WHERE UserId = @UserId", new { _currentUser.UserId });
        var data = await conn.QueryAsync("SELECT * FROM BookSellers WHERE AssignedSalesmanId = @sid AND IsActive = 1 ORDER BY ShopName", new { sid });
        return Ok(ApiResponse<object>.Ok(data));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetBookSeller(int id)
    {
        using var conn = _db.CreateConnection();
        var bs = await conn.QueryFirstOrDefaultAsync(@"
            SELECT b.*, u.FullName AS AssignedSalesmanName
            FROM BookSellers b
            LEFT JOIN Salesmen sm ON b.AssignedSalesmanId = sm.SalesmanId
            LEFT JOIN Users u ON sm.UserId = u.UserId
            WHERE b.BookSellerId = @id AND b.IsActive = 1", new { id });

        return bs is not null ? Ok(ApiResponse<object>.Ok(bs)) : NotFound(ApiResponse<object>.Fail("Bookseller not found."));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> CreateBookSeller([FromBody] CreateBookSellerRequest request)
    {
        using var conn = _db.CreateConnection();
        var id = await conn.QueryFirstOrDefaultAsync<int>(@"
            INSERT INTO BookSellers (ShopName, OwnerName, Phone, Email, Address, City, State, Pincode, AssignedSalesmanId, Latitude, Longitude, IsActive, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.BookSellerId
            VALUES (@ShopName, @OwnerName, @Phone, @Email, @Address, @City, @State, @Pincode, @AssignedSalesmanId, @Latitude, @Longitude, 1, GETUTCDATE(), GETUTCDATE())",
            request);

        return Created($"/api/booksellers/{id}", ApiResponse<object>.Ok(new { BookSellerId = id }, "Bookseller created."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpdateBookSeller(int id, [FromBody] CreateBookSellerRequest request)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(@"
            UPDATE BookSellers SET
                ShopName = COALESCE(@ShopName, ShopName),
                OwnerName = COALESCE(@OwnerName, OwnerName),
                Phone = COALESCE(@Phone, Phone),
                Email = COALESCE(@Email, Email),
                Address = COALESCE(@Address, Address),
                City = COALESCE(@City, City),
                State = COALESCE(@State, State),
                Pincode = COALESCE(@Pincode, Pincode),
                AssignedSalesmanId = COALESCE(@AssignedSalesmanId, AssignedSalesmanId),
                UpdatedAt = GETUTCDATE()
            WHERE BookSellerId = @id", new { request.ShopName, request.OwnerName, request.Phone, request.Email, request.Address, request.City, request.State, request.Pincode, request.AssignedSalesmanId, id });

        return rows > 0 ? Ok(ApiResponse.Ok("Bookseller updated.")) : NotFound(ApiResponse.Fail("Bookseller not found."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteBookSeller(int id)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync("UPDATE BookSellers SET IsActive = 0, UpdatedAt = GETUTCDATE() WHERE BookSellerId = @id", new { id });
        return rows > 0 ? Ok(ApiResponse.Ok("Bookseller deleted.")) : NotFound(ApiResponse.Fail("Bookseller not found."));
    }
}

public class CreateBookSellerRequest
{
    public string ShopName { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Pincode { get; set; }
    public int? AssignedSalesmanId { get; set; }
    public string? Latitude { get; set; }
    public string? Longitude { get; set; }
}
