using Dapper;
using DVR.Application.Common;
using DVR.Application.DTOs.Schools;
using DVR.Application.Features.Schools.Commands;
using DVR.Application.Features.Schools.Queries;
using DVR.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVR.API.Controllers;

[ApiController]
[Route("api/schools")]
[Authorize]
public class SchoolsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IDbConnectionFactory _db;
    private readonly ICurrentUserService _currentUser;

    public SchoolsController(IMediator mediator, IDbConnectionFactory db, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetSchools([FromQuery] SchoolQueryParams query)
    {
        var result = await _mediator.Send(new GetSchoolsQuery { Params = query });
        return Ok(result);
    }

    [HttpGet("my-schools")]
    public async Task<IActionResult> GetMySchools([FromQuery] SchoolQueryParams query)
    {
        using var conn = _db.CreateConnection();
        var salesmanId = await conn.QueryFirstOrDefaultAsync<int?>(
            "SELECT SalesmanId FROM Salesmen WHERE UserId = @UserId", new { _currentUser.UserId });
        query.SalesmanId = salesmanId;
        var result = await _mediator.Send(new GetSchoolsQuery { Params = query });
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetSchool(int id)
    {
        var result = await _mediator.Send(new GetSchoolByIdQuery { SchoolId = id });
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> CreateSchool([FromBody] CreateSchoolRequest request)
    {
        var result = await _mediator.Send(new CreateSchoolCommand { Request = request });
        return result.Success ? Created($"/api/schools/{result.Data?.SchoolId}", result) : BadRequest(result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpdateSchool(int id, [FromBody] UpdateSchoolRequest request)
    {
        var result = await _mediator.Send(new UpdateSchoolCommand { SchoolId = id, Request = request });
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteSchool(int id)
    {
        var result = await _mediator.Send(new DeleteSchoolCommand { SchoolId = id });
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost("{id:int}/contacts")]
    public async Task<IActionResult> AddContact(int id, [FromBody] CreateSchoolContactRequest request)
    {
        using var conn = _db.CreateConnection();
        var school = await conn.QueryFirstOrDefaultAsync<int?>(
            "SELECT SchoolId FROM Schools WHERE SchoolId = @id AND IsActive = 1", new { id });
        if (!school.HasValue)
            return NotFound(ApiResponse.Fail("School not found."));

        var contactId = await conn.QueryFirstOrDefaultAsync<int>(@"
            INSERT INTO SchoolContacts (SchoolId, Name, Designation, Phone, Email, Subject, IsPrimary, IsDeleted, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.ContactId
            VALUES (@SchoolId, @Name, @Designation, @Phone, @Email, @Subject, @IsPrimary, 0, GETUTCDATE(), GETUTCDATE())",
            new { SchoolId = id, request.Name, request.Designation, request.Phone, request.Email, request.Subject, request.IsPrimary });

        var contact = await conn.QueryFirstOrDefaultAsync<SchoolContactDto>(
            "SELECT * FROM SchoolContacts WHERE ContactId = @contactId", new { contactId });

        return Created($"/api/schools/{id}/contacts/{contactId}", ApiResponse<SchoolContactDto>.Ok(contact!));
    }

    [HttpPut("{id:int}/contacts/{contactId:int}")]
    public async Task<IActionResult> UpdateContact(int id, int contactId, [FromBody] CreateSchoolContactRequest request)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(@"
            UPDATE SchoolContacts SET
                Name = @Name, Designation = @Designation, Phone = @Phone,
                Email = @Email, Subject = @Subject, IsPrimary = @IsPrimary, UpdatedAt = GETUTCDATE()
            WHERE ContactId = @contactId AND SchoolId = @id AND IsDeleted = 0",
            new { request.Name, request.Designation, request.Phone, request.Email, request.Subject, request.IsPrimary, contactId, id });

        return rows > 0 ? Ok(ApiResponse.Ok("Contact updated.")) : NotFound(ApiResponse.Fail("Contact not found."));
    }

    // Soft delete — contact stays in DB with IsDeleted=1, can be restored
    [HttpDelete("{id:int}/contacts/{contactId:int}")]
    public async Task<IActionResult> DeleteContact(int id, int contactId)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(
            "UPDATE SchoolContacts SET IsDeleted = 1, DeletedAt = GETUTCDATE(), UpdatedAt = GETUTCDATE() WHERE ContactId = @contactId AND SchoolId = @id",
            new { contactId, id });

        return rows > 0 ? Ok(ApiResponse.Ok("Contact deleted.")) : NotFound(ApiResponse.Fail("Contact not found."));
    }

    // Restore a soft-deleted contact
    [HttpPut("{id:int}/contacts/{contactId:int}/restore")]
    public async Task<IActionResult> RestoreContact(int id, int contactId)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(
            "UPDATE SchoolContacts SET IsDeleted = 0, DeletedAt = NULL, UpdatedAt = GETUTCDATE() WHERE ContactId = @contactId AND SchoolId = @id",
            new { contactId, id });

        return rows > 0 ? Ok(ApiResponse.Ok("Contact restored.")) : NotFound(ApiResponse.Fail("Contact not found."));
    }

    // List all contacts including soft-deleted ones (for admin restore view)
    [HttpGet("{id:int}/contacts/all")]
    public async Task<IActionResult> GetAllContacts(int id)
    {
        using var conn = _db.CreateConnection();
        var contacts = await conn.QueryAsync<SchoolContactDto>(
            "SELECT * FROM SchoolContacts WHERE SchoolId = @id ORDER BY IsDeleted ASC, IsPrimary DESC, ContactId ASC",
            new { id });

        return Ok(ApiResponse<IEnumerable<SchoolContactDto>>.Ok(contacts, "Contacts retrieved."));
    }

    [HttpGet("{id:int}/prescribed-books")]
    public async Task<IActionResult> GetPrescribedBooks(int id)
    {
        using var conn = _db.CreateConnection();
        var books = await conn.QueryAsync(@"
            SELECT spb.Id, spb.SchoolId, spb.BookId, spb.ClassYear, spb.Quantity,
                b.Title, b.Author, b.Subject, b.Series, b.ISBN, b.Price
            FROM SchoolPrescribedBooks spb
            JOIN Books b ON spb.BookId = b.BookId
            WHERE spb.SchoolId = @id", new { id });

        return Ok(ApiResponse<object>.Ok(books, "Prescribed books retrieved."));
    }

    [HttpPut("{id:int}/prescribed-books")]
    public async Task<IActionResult> UpdatePrescribedBooks(int id, [FromBody] List<SchoolPrescribedBookRequest> books)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM SchoolPrescribedBooks WHERE SchoolId = @id", new { id });

        foreach (var book in books)
        {
            await conn.ExecuteAsync(@"
                INSERT INTO SchoolPrescribedBooks (SchoolId, BookId, ClassYear, Quantity, CreatedAt)
                VALUES (@SchoolId, @BookId, @ClassYear, @Quantity, GETUTCDATE())",
                new { SchoolId = id, book.BookId, book.ClassYear, book.Quantity });
        }

        return Ok(ApiResponse.Ok("Prescribed books updated."));
    }
}

public class SchoolPrescribedBookRequest
{
    public int BookId { get; set; }
    public string? ClassYear { get; set; }
    public int? Quantity { get; set; }
}
