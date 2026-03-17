using Dapper;
using DVR.Application.Common;
using DVR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVR.API.Controllers;

[ApiController]
[Route("api/books")]
[Authorize]
public class BooksController : ControllerBase
{
    private readonly IDbConnectionFactory _db;

    public BooksController(IDbConnectionFactory db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetBooks([FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null, [FromQuery] string? subject = null, [FromQuery] string? series = null)
    {
        using var conn = _db.CreateConnection();
        var where = new List<string> { "IsActive = 1" };
        var p = new DynamicParameters();
        p.Add("Offset", (page - 1) * pageSize);
        p.Add("PageSize", pageSize);

        if (!string.IsNullOrWhiteSpace(search)) { where.Add("(Title LIKE @Search OR Author LIKE @Search OR ISBN LIKE @Search)"); p.Add("Search", $"%{search}%"); }
        if (!string.IsNullOrWhiteSpace(subject)) { where.Add("Subject = @Subject"); p.Add("Subject", subject); }
        if (!string.IsNullOrWhiteSpace(series)) { where.Add("Series = @Series"); p.Add("Series", series); }

        var wc = string.Join(" AND ", where);
        var total = await conn.QueryFirstOrDefaultAsync<int>($"SELECT COUNT(*) FROM Books WHERE {wc}", p);
        var data = await conn.QueryAsync($"SELECT * FROM Books WHERE {wc} ORDER BY Title OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY", p);

        return Ok(new PaginatedResponse<object>
        {
            Success = true, Message = "Books retrieved.", Data = data,
            Pagination = new PaginationMeta { Page = page, PageSize = pageSize, TotalCount = total }
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetBook(int id)
    {
        using var conn = _db.CreateConnection();
        var book = await conn.QueryFirstOrDefaultAsync("SELECT * FROM Books WHERE BookId = @id AND IsActive = 1", new { id });
        return book is not null ? Ok(ApiResponse<object>.Ok(book)) : NotFound(ApiResponse<object>.Fail("Book not found."));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateBook([FromBody] CreateBookRequest request)
    {
        using var conn = _db.CreateConnection();
        var id = await conn.QueryFirstOrDefaultAsync<int>(@"
            INSERT INTO Books (Title, Author, Subject, Class, Series, ISBN, Publisher, Price, IsActive, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.BookId
            VALUES (@Title, @Author, @Subject, @Class, @Series, @ISBN, @Publisher, @Price, 1, GETUTCDATE(), GETUTCDATE())",
            request);
        return Created($"/api/books/{id}", ApiResponse<object>.Ok(new { BookId = id }, "Book created."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateBook(int id, [FromBody] CreateBookRequest request)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(@"
            UPDATE Books SET Title = @Title, Author = @Author, Subject = @Subject,
                Class = @Class, Series = @Series, ISBN = @ISBN, Publisher = @Publisher,
                Price = @Price, UpdatedAt = GETUTCDATE()
            WHERE BookId = @id",
            new { request.Title, request.Author, request.Subject, request.Class, request.Series, request.ISBN, request.Publisher, request.Price, id });

        return rows > 0 ? Ok(ApiResponse.Ok("Book updated.")) : NotFound(ApiResponse.Fail("Book not found."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteBook(int id)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync("UPDATE Books SET IsActive = 0, UpdatedAt = GETUTCDATE() WHERE BookId = @id", new { id });
        return rows > 0 ? Ok(ApiResponse.Ok("Book deleted.")) : NotFound(ApiResponse.Fail("Book not found."));
    }
}

public class CreateBookRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Author { get; set; }
    public string? Subject { get; set; }
    public string? Class { get; set; }
    public string? Series { get; set; }
    public string? ISBN { get; set; }
    public string? Publisher { get; set; }
    public decimal? Price { get; set; }
}
