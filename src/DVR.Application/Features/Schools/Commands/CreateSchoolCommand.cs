using Dapper;
using DVR.Application.Common;
using DVR.Application.DTOs.Schools;
using DVR.Application.Interfaces;
using MediatR;

namespace DVR.Application.Features.Schools.Commands;

public class CreateSchoolCommand : IRequest<ApiResponse<SchoolDto>>
{
    public CreateSchoolRequest Request { get; set; } = new();
}

public class CreateSchoolCommandHandler : IRequestHandler<CreateSchoolCommand, ApiResponse<SchoolDto>>
{
    private readonly IDbConnectionFactory _db;

    public CreateSchoolCommandHandler(IDbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<ApiResponse<SchoolDto>> Handle(CreateSchoolCommand command, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        var r = command.Request;

        var id = await conn.QueryFirstOrDefaultAsync<int>(@"
            INSERT INTO Schools (SchoolName, PrincipalName, Phone, Email, Address, City, State, Pincode,
                SchoolType, Board, TotalStudents, Category, AssignedSalesmanId, Latitude, Longitude, SalesTarget, PrescribeSubjects, IsActive, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.SchoolId
            VALUES (@SchoolName, @PrincipalName, @Phone, @Email, @Address, @City, @State, @Pincode,
                @SchoolType, @Board, @TotalStudents, @Category, @AssignedSalesmanId, @Latitude, @Longitude, @SalesTarget, @PrescribeSubjects, 1, GETUTCDATE(), GETUTCDATE())",
            r);

        // Save prescribed books
        if (r.PrescribedBookIds?.Count > 0)
        {
            foreach (var bookId in r.PrescribedBookIds)
            {
                await conn.ExecuteAsync(
                    "INSERT INTO SchoolPrescribedBooks (SchoolId, BookId, CreatedAt) VALUES (@SchoolId, @BookId, GETUTCDATE())",
                    new { SchoolId = id, BookId = bookId });
            }
        }

        var school = await conn.QueryFirstOrDefaultAsync<SchoolDto>(
            "SELECT * FROM Schools WHERE SchoolId = @id", new { id });

        return ApiResponse<SchoolDto>.Ok(school!, "School created successfully.");
    }
}
