using Dapper;
using DVR.Application.Common;
using DVR.Application.DTOs.Schools;
using DVR.Application.Interfaces;
using MediatR;

namespace DVR.Application.Features.Schools.Queries;

public class GetSchoolByIdQuery : IRequest<ApiResponse<SchoolDto>>
{
    public int SchoolId { get; set; }
}

public class GetSchoolByIdQueryHandler : IRequestHandler<GetSchoolByIdQuery, ApiResponse<SchoolDto>>
{
    private readonly IDbConnectionFactory _db;

    public GetSchoolByIdQueryHandler(IDbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<ApiResponse<SchoolDto>> Handle(GetSchoolByIdQuery request, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();

        var school = await conn.QueryFirstOrDefaultAsync<SchoolDto>(@"
            SELECT s.*, u.FullName AS AssignedSalesmanName
            FROM Schools s
            LEFT JOIN Salesmen sm ON s.AssignedSalesmanId = sm.SalesmanId
            LEFT JOIN Users u ON sm.UserId = u.UserId
            WHERE s.SchoolId = @SchoolId AND s.IsActive = 1",
            new { request.SchoolId });

        if (school is null)
            return ApiResponse<SchoolDto>.Fail("School not found.");

        var contacts = await conn.QueryAsync<SchoolContactDto>(
            "SELECT * FROM SchoolContacts WHERE SchoolId = @SchoolId AND IsDeleted = 0 ORDER BY IsPrimary DESC, ContactId ASC",
            new { request.SchoolId });

        school.Contacts = contacts.ToList();

        var prescribedBooks = await conn.QueryAsync<SchoolPrescribedBookDto>(@"
            SELECT spb.Id, spb.SchoolId, spb.BookId, spb.ClassYear, spb.Quantity,
                   b.Title, b.Subject
            FROM SchoolPrescribedBooks spb
            JOIN Books b ON spb.BookId = b.BookId
            WHERE spb.SchoolId = @SchoolId",
            new { request.SchoolId });

        school.PrescribedBooks = prescribedBooks.ToList();

        return ApiResponse<SchoolDto>.Ok(school);
    }
}
